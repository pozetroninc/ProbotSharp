# AWS ECS Deployment Guide

This guide covers deploying ProbotSharp to AWS using Amazon ECS (Elastic Container Service) with Fargate.

## Architecture Overview

The AWS deployment includes:
- **Amazon ECS Fargate** - Serverless container orchestration
- **Application Load Balancer (ALB)** - HTTPS traffic routing with SSL/TLS termination
- **Amazon RDS PostgreSQL** - Managed database service
- **Amazon ElastiCache Redis** - Managed in-memory cache
- **AWS Secrets Manager** - Secure credential storage
- **Amazon ECR** - Container image registry
- **CloudWatch Logs** - Centralized logging
- **VPC with public/private subnets** - Network isolation

## Prerequisites

### Required Tools

- [AWS CLI](https://aws.amazon.com/cli/) v2.x or later
- [Docker](https://www.docker.com/products/docker-desktop)
- AWS account with appropriate permissions
- Domain name (optional, for custom domain)
- SSL/TLS certificate in AWS Certificate Manager (for HTTPS)

### AWS Permissions Required

Your IAM user/role needs permissions for:
- ECS (task definitions, services, clusters)
- EC2 (VPC, security groups, subnets, load balancers)
- RDS (database instances, subnet groups)
- ElastiCache (Redis clusters, subnet groups)
- Secrets Manager (create/read secrets)
- ECR (push/pull container images)
- CloudFormation (create/update stacks)
- IAM (create roles and policies)
- CloudWatch Logs (create log groups)

### GitHub App Credentials

Before deploying, you need:
- GitHub App ID
- GitHub App Private Key (PEM format)
- GitHub Webhook Secret

See the [Local Development Guide](../LocalDevelopment.md#2-create-a-github-app) for instructions on creating a GitHub App.

## Deployment Methods

Choose one of the following deployment methods:

### Method 1: CloudFormation (Recommended for Production)

This method uses Infrastructure as Code to provision all resources.

#### Step 1: Prepare Parameters

The CloudFormation template requires several parameters. Create a parameters file or prepare to enter them interactively.

Required parameters:
- `VpcId` - Your VPC ID (e.g., `vpc-abc123`)
- `PublicSubnetIds` - Comma-separated list of public subnet IDs for ALB
- `PrivateSubnetIds` - Comma-separated list of private subnet IDs for ECS/RDS/Redis
- `CertificateArn` - ACM certificate ARN for HTTPS (e.g., `arn:aws:acm:us-east-1:123456789012:certificate/abc123`)
- `GitHubAppId` - Your GitHub App ID
- `GitHubWebhookSecret` - Your webhook secret
- `GitHubPrivateKey` - Your private key (PEM format)
- `DatabasePassword` - Strong password for PostgreSQL (min 8 characters)

#### Step 2: Create the CloudFormation Stack

```bash
aws cloudformation create-stack \
  --stack-name probotsharp-production \
  --template-body file://deploy/aws/cloudformation.yaml \
  --parameters \
    ParameterKey=Environment,ParameterValue=production \
    ParameterKey=VpcId,ParameterValue=vpc-your-vpc-id \
    ParameterKey=PublicSubnetIds,ParameterValue=subnet-pub1,subnet-pub2 \
    ParameterKey=PrivateSubnetIds,ParameterValue=subnet-priv1,subnet-priv2 \
    ParameterKey=CertificateArn,ParameterValue=arn:aws:acm:region:account:certificate/id \
    ParameterKey=GitHubAppId,ParameterValue=123456 \
    ParameterKey=GitHubWebhookSecret,ParameterValue=your-webhook-secret \
    ParameterKey=GitHubPrivateKey,ParameterValue="$(cat github-app-key.pem)" \
    ParameterKey=DatabasePassword,ParameterValue=your-database-password \
  --capabilities CAPABILITY_NAMED_IAM \
  --region us-east-1
```

#### Step 3: Wait for Stack Creation

Monitor the stack creation progress:

```bash
aws cloudformation wait stack-create-complete \
  --stack-name probotsharp-production \
  --region us-east-1
```

This typically takes 10-15 minutes.

#### Step 4: Get Stack Outputs

Retrieve important information about your deployment:

```bash
aws cloudformation describe-stacks \
  --stack-name probotsharp-production \
  --region us-east-1 \
  --query 'Stacks[0].Outputs'
```

Note the following outputs:
- `LoadBalancerDNS` - Your application's URL
- `ECRRepositoryUri` - Where to push Docker images
- `ECSClusterName` - Cluster name for service deployment
- `DatabaseEndpoint` - RDS endpoint (for manual access)
- `RedisEndpoint` - Redis endpoint (for manual access)

#### Step 5: Build and Push Docker Image

```bash
# Get ECR repository URI from stack outputs
ECR_REPO=$(aws cloudformation describe-stacks \
  --stack-name probotsharp-production \
  --query 'Stacks[0].Outputs[?OutputKey==`ECRRepositoryUri`].OutputValue' \
  --output text \
  --region us-east-1)

# Authenticate Docker to ECR
aws ecr get-login-password --region us-east-1 | \
  docker login --username AWS --password-stdin $ECR_REPO

# Build and tag image
docker build -t probotsharp:latest .
docker tag probotsharp:latest $ECR_REPO:latest
docker tag probotsharp:latest $ECR_REPO:$(git rev-parse --short HEAD)

# Push to ECR
docker push $ECR_REPO:latest
docker push $ECR_REPO:$(git rev-parse --short HEAD)
```

#### Step 6: Create ECS Service

After the infrastructure is created and the image is pushed, create the ECS service:

```bash
# Update task definition with your ECR image URI
sed "s|\${ECR_REPOSITORY_URI}|$ECR_REPO|g" deploy/aws/task-definition.json | \
sed "s|\${IMAGE_TAG}|latest|g" | \
sed "s|\${AWS_ACCOUNT_ID}|$(aws sts get-caller-identity --query Account --output text)|g" | \
sed "s|\${AWS_REGION}|us-east-1|g" > task-definition-resolved.json

# Register task definition
TASK_DEF_ARN=$(aws ecs register-task-definition \
  --cli-input-json file://task-definition-resolved.json \
  --region us-east-1 \
  --query 'taskDefinition.taskDefinitionArn' \
  --output text)

# Create ECS service
aws ecs create-service \
  --cluster probotsharp-production-cluster \
  --service-name probotsharp-service \
  --task-definition $TASK_DEF_ARN \
  --desired-count 2 \
  --launch-type FARGATE \
  --network-configuration "awsvpcConfiguration={subnets=[subnet-priv1,subnet-priv2],securityGroups=[sg-ecs],assignPublicIp=DISABLED}" \
  --load-balancers "targetGroupArn=arn:aws:elasticloadbalancing:region:account:targetgroup/name,containerName=probotsharp,containerPort=8080" \
  --region us-east-1
```

#### Step 7: Run Database Migrations

Run migrations from your local machine or a bastion host:

```bash
# Get database connection string from Secrets Manager
DB_PASSWORD=$(aws secretsmanager get-secret-value \
  --secret-id probotsharp-production/database/connection-string \
  --query SecretString \
  --output text \
  --region us-east-1)

# Run migrations
dotnet ef database update \
  --project src/ProbotSharp.Infrastructure \
  --startup-project src/ProbotSharp.Bootstrap.Api \
  --connection "$DB_PASSWORD"
```

Or use ECS Exec to run migrations from within a task:

```bash
aws ecs execute-command \
  --cluster probotsharp-production-cluster \
  --task <task-id> \
  --container probotsharp \
  --interactive \
  --command "/bin/sh"

# Inside the container:
dotnet ef database update --project /app/ProbotSharp.Infrastructure.dll
```

#### Step 8: Configure GitHub Webhook URL

1. Get your load balancer DNS name from the CloudFormation outputs
2. Go to your GitHub App settings
3. Update the **Webhook URL** to `https://<load-balancer-dns>/webhooks`
4. Save changes

#### Step 9: Verify Deployment

Test your deployment:

```bash
# Health check
curl https://<load-balancer-dns>/health

# Root endpoint
curl https://<load-balancer-dns>/
```

### Method 2: GitHub Actions (Automated CI/CD)

This method uses GitHub Actions for automated deployments on every push to `main`.

#### Step 1: Configure AWS Credentials

Set up OIDC authentication (recommended) or use access keys:

**Option A: OIDC (Recommended)**

1. Create an IAM OIDC identity provider for GitHub:

```bash
aws iam create-open-id-connect-provider \
  --url https://token.actions.githubusercontent.com \
  --client-id-list sts.amazonaws.com \
  --thumbprint-list 6938fd4d98bab03faadb97b34396831e3780aea1
```

2. Create an IAM role with trust policy:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Principal": {
        "Federated": "arn:aws:iam::ACCOUNT_ID:oidc-provider/token.actions.githubusercontent.com"
      },
      "Action": "sts:AssumeRoleWithWebIdentity",
      "Condition": {
        "StringEquals": {
          "token.actions.githubusercontent.com:aud": "sts.amazonaws.com"
        },
        "StringLike": {
          "token.actions.githubusercontent.com:sub": "repo:yourusername/probotsharp:*"
        }
      }
    }
  ]
}
```

3. Attach necessary policies to the role (ECS, ECR, Secrets Manager access)

**Option B: Access Keys**

Create an IAM user with programmatic access and necessary permissions.

#### Step 2: Configure GitHub Secrets

Go to your repository settings → Secrets and variables → Actions, and add:

- `AWS_ROLE_ARN` - IAM role ARN (for OIDC) or leave empty if using access keys
- `AWS_ACCESS_KEY_ID` - Access key ID (if not using OIDC)
- `AWS_SECRET_ACCESS_KEY` - Secret access key (if not using OIDC)
- `DATABASE_CONNECTION_STRING` - RDS connection string

#### Step 3: Deploy Infrastructure First

Before the GitHub Actions workflow can deploy, you must first create the infrastructure using Method 1 (CloudFormation).

#### Step 4: Push to Main Branch

Once infrastructure exists, every push to `main` will trigger automatic deployment:

```bash
git push origin main
```

The workflow will:
1. Build the Docker image
2. Push to ECR
3. Update ECS task definition
4. Deploy new task to ECS service
5. Run database migrations
6. Wait for deployment to stabilize

#### Step 5: Monitor Deployment

View the deployment progress in the GitHub Actions tab of your repository.

## Configuration

### Environment Variables

The ECS task definition sets environment variables from:
- **Direct values** - Non-sensitive configuration
- **Secrets Manager** - Sensitive credentials

Key configuration sections:
- `ConnectionStrings__ProbotSharp` - Database connection
- `ProbotSharp__GitHub__*` - GitHub App credentials
- `ProbotSharp__Cache__RedisConnectionString` - Redis connection
- `ProbotSharp__Metrics__OtlpEndpoint` - Observability endpoint

### Scaling Configuration

**Task Definition Resources:**
- CPU: 512 (0.5 vCPU)
- Memory: 1024 MB (1 GB)

Adjust in `deploy/aws/task-definition.json` if needed.

**Auto Scaling (Optional):**

Create auto-scaling policies:

```bash
# Register scalable target
aws application-autoscaling register-scalable-target \
  --service-namespace ecs \
  --resource-id service/probotsharp-production-cluster/probotsharp-service \
  --scalable-dimension ecs:service:DesiredCount \
  --min-capacity 2 \
  --max-capacity 10 \
  --region us-east-1

# Create scaling policy
aws application-autoscaling put-scaling-policy \
  --service-namespace ecs \
  --resource-id service/probotsharp-production-cluster/probotsharp-service \
  --scalable-dimension ecs:service:DesiredCount \
  --policy-name cpu-scaling-policy \
  --policy-type TargetTrackingScaling \
  --target-tracking-scaling-policy-configuration file://scaling-policy.json \
  --region us-east-1
```

scaling-policy.json:
```json
{
  "TargetValue": 70.0,
  "PredefinedMetricSpecification": {
    "PredefinedMetricType": "ECSServiceAverageCPUUtilization"
  },
  "ScaleInCooldown": 300,
  "ScaleOutCooldown": 60
}
```

## Monitoring and Logging

### CloudWatch Logs

View application logs:

```bash
aws logs tail /ecs/probotsharp-production --follow --region us-east-1
```

Or use CloudWatch Insights queries:

```bash
aws logs start-query \
  --log-group-name /ecs/probotsharp-production \
  --start-time $(date -d '1 hour ago' +%s) \
  --end-time $(date +%s) \
  --query-string 'fields @timestamp, @message | filter @message like /ERROR/ | sort @timestamp desc | limit 20'
```

### Health Checks

The ALB performs health checks on `/health` every 30 seconds.

View task health:

```bash
aws ecs describe-services \
  --cluster probotsharp-production-cluster \
  --services probotsharp-service \
  --region us-east-1 \
  --query 'services[0].{Running:runningCount,Desired:desiredCount,Healthy:healthCheckGracePeriodSeconds}'
```

### Metrics

Key CloudWatch metrics to monitor:
- `CPUUtilization` - Task CPU usage
- `MemoryUtilization` - Task memory usage
- `TargetResponseTime` - ALB target response time
- `HealthyHostCount` - Number of healthy tasks
- `HTTPCode_Target_4XX_Count` - Client errors
- `HTTPCode_Target_5XX_Count` - Server errors

Create CloudWatch dashboard:

```bash
aws cloudwatch put-dashboard \
  --dashboard-name probotsharp-production \
  --dashboard-body file://dashboard.json
```

## Troubleshooting

### Task Keeps Restarting

Check task logs:

```bash
# Get task ID
TASK_ID=$(aws ecs list-tasks \
  --cluster probotsharp-production-cluster \
  --service probotsharp-service \
  --query 'taskArns[0]' \
  --output text | cut -d'/' -f3)

# View task details
aws ecs describe-tasks \
  --cluster probotsharp-production-cluster \
  --tasks $TASK_ID

# View logs
aws logs tail /ecs/probotsharp-production --follow --region us-east-1
```

Common issues:
- **Database connection failure** - Check security group rules, verify RDS is accessible from ECS security group
- **Secrets Manager access denied** - Verify task execution role has `secretsmanager:GetSecretValue` permission
- **Image pull failure** - Ensure ECR permissions are correct

### Database Connection Issues

Test database connectivity from ECS task:

```bash
aws ecs execute-command \
  --cluster probotsharp-production-cluster \
  --task $TASK_ID \
  --container probotsharp \
  --interactive \
  --command "/bin/sh"

# Inside container:
apt-get update && apt-get install -y postgresql-client
psql -h <rds-endpoint> -U probotsharp -d probotsharp
```

### High CPU/Memory Usage

Scale up task resources:

1. Update task definition CPU/memory values
2. Register new task definition revision
3. Update service to use new revision

```bash
aws ecs update-service \
  --cluster probotsharp-production-cluster \
  --service probotsharp-service \
  --task-definition probotsharp:2 \
  --force-new-deployment
```

## Updating the Application

### Rolling Updates

Update service with new task definition:

```bash
aws ecs update-service \
  --cluster probotsharp-production-cluster \
  --service probotsharp-service \
  --task-definition probotsharp:latest \
  --force-new-deployment
```

ECS will perform a rolling update, replacing tasks one at a time.

### Blue/Green Deployments (Advanced)

Use AWS CodeDeploy for blue/green deployments with automatic rollback:

1. Create CodeDeploy application and deployment group
2. Configure deployment configuration (linear, canary, or all-at-once)
3. Trigger deployment via CodeDeploy API or GitHub Actions

## Costs

Estimated monthly costs for minimal production setup:
- **ECS Fargate (2 tasks, 0.5 vCPU, 1GB)** - ~$30
- **ALB** - ~$20
- **RDS PostgreSQL (db.t4g.micro)** - ~$15
- **ElastiCache Redis (cache.t4g.micro)** - ~$12
- **Data transfer** - ~$5-10
- **CloudWatch Logs** - ~$5
- **Secrets Manager** - ~$2

**Total: ~$85-90/month**

Costs increase with:
- More ECS tasks
- Larger RDS/Redis instances
- Higher traffic (data transfer)
- Log retention

## Cleanup

Delete all resources:

```bash
# Delete ECS service first
aws ecs update-service \
  --cluster probotsharp-production-cluster \
  --service probotsharp-service \
  --desired-count 0

aws ecs delete-service \
  --cluster probotsharp-production-cluster \
  --service probotsharp-service

# Delete CloudFormation stack
aws cloudformation delete-stack \
  --stack-name probotsharp-production

# Monitor deletion
aws cloudformation wait stack-delete-complete \
  --stack-name probotsharp-production
```

**Note**: RDS instances have deletion protection enabled. Disable it first:

```bash
aws rds modify-db-instance \
  --db-instance-identifier probotsharp-production-postgres \
  --no-deletion-protection
```

## Next Steps

- Set up [CloudWatch Alarms](https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/AlarmThatSendsEmail.html) for critical metrics
- Configure [AWS WAF](https://aws.amazon.com/waf/) for application firewall protection
- Enable [AWS X-Ray](https://aws.amazon.com/xray/) for distributed tracing
- Set up [Route 53](https://aws.amazon.com/route53/) for custom domain
- Implement [backup strategy](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/USER_WorkingWithAutomatedBackups.html) for RDS
