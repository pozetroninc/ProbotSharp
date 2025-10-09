# AWS ECS Deployment Guide

This guide covers deploying ProbotSharp to Amazon Web Services (AWS) using Elastic Container Service (ECS) with Fargate.

## Architecture Overview

- **Compute**: ECS Fargate (serverless containers)
- **Container Registry**: Amazon ECR
- **Database**: Amazon RDS PostgreSQL
- **Cache**: Amazon ElastiCache Redis
- **Secrets**: AWS Secrets Manager
- **Logging**: CloudWatch Logs
- **Networking**: VPC with public and private subnets

## Prerequisites

Before deploying, ensure you have the following AWS resources:

1. **AWS Account** with appropriate permissions
2. **ECR Repository** for container images
3. **ECS Cluster** (Fargate compatible)
4. **RDS PostgreSQL Instance** (version 13 or higher)
5. **ElastiCache Redis Cluster** (version 6.x or higher)
6. **VPC with Subnets** (public for ALB, private for ECS tasks)
7. **Application Load Balancer** (optional but recommended)
8. **IAM Roles**:
   - `ecsTaskExecutionRole` - for ECS to pull images and read secrets
   - `probotsharp-task-role` - for application to access AWS services

### Quick Infrastructure Setup

Use the provided CloudFormation template to create all required infrastructure:

```bash
aws cloudformation create-stack \
  --stack-name probotsharp-infrastructure \
  --template-body file://cloudformation.yaml \
  --capabilities CAPABILITY_IAM \
  --parameters \
    ParameterKey=EnvironmentName,ParameterValue=production \
    ParameterKey=DatabasePassword,ParameterValue=YourSecurePassword123!
```

## Required Secrets

Store the following secrets in AWS Secrets Manager:

### 1. Database Connection String
```bash
aws secretsmanager create-secret \
  --name probotsharp/database \
  --description "ProbotSharp PostgreSQL connection string" \
  --secret-string '{"connection_string":"Host=your-rds-endpoint.region.rds.amazonaws.com;Port=5432;Database=probotsharp;Username=probotsharp;Password=YourPassword"}'
```

### 2. GitHub App ID
```bash
aws secretsmanager create-secret \
  --name probotsharp/github \
  --description "GitHub App credentials" \
  --secret-string '{"app_id":"123456","webhook_secret":"your-webhook-secret","private_key":"-----BEGIN RSA PRIVATE KEY-----\n...\n-----END RSA PRIVATE KEY-----"}'
```

### 3. Redis Connection String
```bash
aws secretsmanager create-secret \
  --name probotsharp/redis \
  --description "ElastiCache Redis connection string" \
  --secret-string '{"connection_string":"your-elasticache-endpoint.cache.amazonaws.com:6379"}'
```

### 4. Metrics Endpoint (Optional)
```bash
aws secretsmanager create-secret \
  --name probotsharp/metrics \
  --description "OpenTelemetry collector endpoint" \
  --secret-string '{"otlp_endpoint":"http://otel-collector:4317"}'
```

## Environment Variables

The ECS task definition includes these non-secret environment variables:

| Variable | Value | Description |
|----------|-------|-------------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | ASP.NET Core environment |
| `ASPNETCORE_URLS` | `http://+:8080` | Port binding |
| `ProbotSharp__Adapters__Cache__Provider` | `Redis` | Cache adapter provider |
| `ProbotSharp__Adapters__Idempotency__Provider` | `Redis` | Idempotency adapter provider |
| `ProbotSharp__Adapters__Persistence__Provider` | `PostgreSQL` | Persistence adapter provider |
| `ProbotSharp__Adapters__ReplayQueue__Provider` | `InMemory` | Replay queue adapter provider |
| `ProbotSharp__Adapters__DeadLetterQueue__Provider` | `Database` | Dead letter queue adapter provider |
| `ProbotSharp__Adapters__Metrics__Provider` | `OpenTelemetry` | Metrics adapter provider |
| `ProbotSharp__Adapters__Tracing__Provider` | `OpenTelemetry` | Tracing adapter provider |

## GitHub Actions Deployment

### Setup GitHub Secrets

Configure the following secrets in your GitHub repository settings:

1. **AWS_ROLE_ARN**: ARN of the IAM role for GitHub Actions OIDC authentication
   - Example: `arn:aws:iam::123456789012:role/GitHubActionsDeploymentRole`
   - This role should have permissions to push to ECR and update ECS services

### Setting up OIDC Authentication

Create an IAM OIDC identity provider for GitHub:

```bash
# Create the identity provider (one-time setup)
aws iam create-open-id-connect-provider \
  --url https://token.actions.githubusercontent.com \
  --client-id-list sts.amazonaws.com \
  --thumbprint-list 6938fd4d98bab03faadb97b34396831e3780aea1
```

Create an IAM role with trust policy:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Principal": {
        "Federated": "arn:aws:iam::YOUR_ACCOUNT_ID:oidc-provider/token.actions.githubusercontent.com"
      },
      "Action": "sts:AssumeRoleWithWebIdentity",
      "Condition": {
        "StringEquals": {
          "token.actions.githubusercontent.com:aud": "sts.amazonaws.com"
        },
        "StringLike": {
          "token.actions.githubusercontent.com:sub": "repo:YOUR_ORG/YOUR_REPO:*"
        }
      }
    }
  ]
}
```

Attach permissions policy to the role:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "ecr:GetAuthorizationToken",
        "ecr:BatchCheckLayerAvailability",
        "ecr:GetDownloadUrlForLayer",
        "ecr:PutImage",
        "ecr:InitiateLayerUpload",
        "ecr:UploadLayerPart",
        "ecr:CompleteLayerUpload"
      ],
      "Resource": "*"
    },
    {
      "Effect": "Allow",
      "Action": [
        "ecs:UpdateService",
        "ecs:DescribeServices",
        "ecs:DescribeTaskDefinition",
        "ecs:RegisterTaskDefinition"
      ],
      "Resource": "*"
    },
    {
      "Effect": "Allow",
      "Action": "iam:PassRole",
      "Resource": [
        "arn:aws:iam::*:role/ecsTaskExecutionRole",
        "arn:aws:iam::*:role/probotsharp-task-role"
      ]
    }
  ]
}
```

### Trigger Deployment

The workflow automatically deploys on push to `main` branch:

```bash
git push origin main
```

Or manually trigger:

```bash
gh workflow run deploy-aws.yml
```

## Manual Deployment

### 1. Build and Push Docker Image

```bash
# Authenticate to ECR
aws ecr get-login-password --region us-east-1 | \
  docker login --username AWS --password-stdin YOUR_ACCOUNT_ID.dkr.ecr.us-east-1.amazonaws.com

# Build image from repository root
cd /path/to/probot-sharp
docker build -f src/ProbotSharp.Bootstrap.Api/Dockerfile -t probotsharp:latest .

# Tag and push
docker tag probotsharp:latest YOUR_ACCOUNT_ID.dkr.ecr.us-east-1.amazonaws.com/probotsharp:latest
docker push YOUR_ACCOUNT_ID.dkr.ecr.us-east-1.amazonaws.com/probotsharp:latest
```

### 2. Update Task Definition

Update the placeholders in `ecs-task-definition.json`:

```bash
# Replace placeholders
export AWS_ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
export AWS_REGION=us-east-1
export ECR_REPOSITORY_URI=$AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/probotsharp
export IMAGE_TAG=latest

# Create task definition with substitutions
envsubst < deploy/aws/ecs-task-definition.json > /tmp/task-def.json

# Register task definition
aws ecs register-task-definition --cli-input-json file:///tmp/task-def.json
```

### 3. Update ECS Service

```bash
aws ecs update-service \
  --cluster probotsharp-cluster \
  --service probotsharp-service \
  --task-definition probotsharp \
  --force-new-deployment
```

### 4. Run Database Migrations

```bash
# Get the connection string from Secrets Manager
CONNECTION_STRING=$(aws secretsmanager get-secret-value \
  --secret-id probotsharp/database \
  --query SecretString --output text | jq -r .connection_string)

# Install EF Core tools
dotnet tool install --global dotnet-ef

# Run migrations
dotnet ef database update \
  --project src/ProbotSharp.Infrastructure \
  --startup-project src/ProbotSharp.Bootstrap.Api \
  --connection "$CONNECTION_STRING"
```

## Monitoring and Logs

### View Application Logs

```bash
# View logs in CloudWatch
aws logs tail /ecs/probotsharp --follow

# Get recent errors
aws logs filter-log-events \
  --log-group-name /ecs/probotsharp \
  --filter-pattern "ERROR" \
  --start-time $(date -d '1 hour ago' +%s)000
```

### Check Service Status

```bash
# Check service health
aws ecs describe-services \
  --cluster probotsharp-cluster \
  --services probotsharp-service

# List running tasks
aws ecs list-tasks \
  --cluster probotsharp-cluster \
  --service-name probotsharp-service

# Get task details
aws ecs describe-tasks \
  --cluster probotsharp-cluster \
  --tasks TASK_ARN
```

### Health Check Endpoint

The application exposes a health check at `http://your-alb-dns/health`

```bash
curl http://your-alb-dns/health
```

## Troubleshooting

### Container Fails to Start

1. **Check CloudWatch Logs** for application errors:
   ```bash
   aws logs tail /ecs/probotsharp --follow
   ```

2. **Verify Secrets** are correctly configured in Secrets Manager

3. **Check Task Execution Role** has permission to read secrets:
   ```bash
   aws iam get-role --role-name ecsTaskExecutionRole
   ```

### Database Connection Issues

1. **Verify Security Groups** allow ECS tasks to reach RDS:
   - ECS security group should allow outbound on port 5432
   - RDS security group should allow inbound from ECS security group

2. **Test connectivity** from ECS task:
   ```bash
   aws ecs execute-command \
     --cluster probotsharp-cluster \
     --task TASK_ID \
     --container probotsharp \
     --interactive \
     --command "/bin/sh"

   # Inside container
   nc -zv your-rds-endpoint.rds.amazonaws.com 5432
   ```

3. **Check connection string** format in Secrets Manager

### Redis Connection Issues

1. **Verify ElastiCache endpoint** is correct in secrets

2. **Check Security Groups** allow ECS to reach ElastiCache on port 6379

3. **Verify Redis is in same VPC** as ECS tasks

### Task Definition Registration Fails

1. **Validate JSON syntax**:
   ```bash
   cat deploy/aws/ecs-task-definition.json | jq .
   ```

2. **Check IAM role ARNs** exist and are correct

3. **Verify CPU/memory combinations** are valid for Fargate:
   - Valid pairs: 256/512, 512/1024, 1024/2048, 2048/4096, etc.

### Deployment Times Out

1. **Increase deployment timeout** in workflow (default is wait-for-service-stability)

2. **Check health check configuration** - may need longer `startPeriod`

3. **Review deployment circuit breaker** settings in ECS service

### High Memory Usage

1. **Monitor CloudWatch metrics** for container memory

2. **Increase task memory** in task definition if needed

3. **Check for memory leaks** in application logs

## Scaling Configuration

### Manual Scaling

Update the service desired count:

```bash
aws ecs update-service \
  --cluster probotsharp-cluster \
  --service probotsharp-service \
  --desired-count 3
```

### Auto Scaling

Create Application Auto Scaling target:

```bash
aws application-autoscaling register-scalable-target \
  --service-namespace ecs \
  --resource-id service/probotsharp-cluster/probotsharp-service \
  --scalable-dimension ecs:service:DesiredCount \
  --min-capacity 2 \
  --max-capacity 10

aws application-autoscaling put-scaling-policy \
  --service-namespace ecs \
  --resource-id service/probotsharp-cluster/probotsharp-service \
  --scalable-dimension ecs:service:DesiredCount \
  --policy-name cpu-scaling-policy \
  --policy-type TargetTrackingScaling \
  --target-tracking-scaling-policy-configuration file://scaling-policy.json
```

Example `scaling-policy.json`:

```json
{
  "TargetValue": 70.0,
  "PredefinedMetricSpecification": {
    "PredefinedMetricType": "ECSServiceAverageCPUUtilization"
  },
  "ScaleOutCooldown": 60,
  "ScaleInCooldown": 300
}
```

## Cost Optimization

1. **Use Fargate Spot** for non-production workloads
2. **Right-size task CPU/memory** based on actual usage
3. **Use RDS reserved instances** for predictable workloads
4. **Enable ElastiCache auto-scaling** for variable load
5. **Set up CloudWatch alarms** for cost anomalies

## Security Best Practices

1. **Enable VPC Flow Logs** for network monitoring
2. **Use AWS WAF** with Application Load Balancer
3. **Enable GuardDuty** for threat detection
4. **Rotate secrets regularly** using Secrets Manager rotation
5. **Enable container insights** for enhanced monitoring
6. **Use least privilege IAM policies**
7. **Enable encryption at rest** for RDS and ElastiCache
8. **Use AWS Systems Manager Session Manager** instead of SSH

## Additional Resources

- [AWS ECS Documentation](https://docs.aws.amazon.com/ecs/)
- [ECS Task Definition Parameters](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/task_definition_parameters.html)
- [AWS Secrets Manager](https://docs.aws.amazon.com/secretsmanager/)
- [CloudFormation Template Reference](https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/)
