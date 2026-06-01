#!/bin/bash
# ============================================================
# UNI-EDU Backend — Deploy to AWS ECS Fargate
# ============================================================
# Usage:
#   chmod +x aws/deploy.sh
#   ./aws/deploy.sh
#
# Prerequisites: AWS CLI v2 configured, Docker running
# ============================================================

set -euo pipefail

# ── Configuration ──────────────────────────────────────────
AWS_REGION="ap-southeast-1"           # Singapore (gần VN nhất)
AWS_ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
ECR_REPO="uni-edu-api"
ECS_CLUSTER="uni-edu-cluster"
ECS_SERVICE="uni-edu-api-service"
IMAGE_TAG="latest"

ECR_URI="${AWS_ACCOUNT_ID}.dkr.ecr.${AWS_REGION}.amazonaws.com/${ECR_REPO}"

echo "==> Account: ${AWS_ACCOUNT_ID}"
echo "==> Region:  ${AWS_REGION}"
echo "==> ECR:     ${ECR_URI}"

# ── Step 1: Login to ECR ───────────────────────────────────
echo "==> Logging in to ECR..."
aws ecr get-login-password --region ${AWS_REGION} \
  | docker login --username AWS --password-stdin ${ECR_URI}

# ── Step 2: Build Docker image ─────────────────────────────
echo "==> Building Docker image..."
docker build -t ${ECR_REPO}:${IMAGE_TAG} .

# ── Step 3: Tag & Push to ECR ──────────────────────────────
echo "==> Pushing to ECR..."
docker tag ${ECR_REPO}:${IMAGE_TAG} ${ECR_URI}:${IMAGE_TAG}
docker push ${ECR_URI}:${IMAGE_TAG}

# ── Step 4: Register new task definition revision ─────────
echo "==> Registering task definition..."
aws ecs register-task-definition \
  --cli-input-json file://aws/task-definition.json \
  --region ${AWS_REGION} \
  > /dev/null
echo "    Registered new revision of uni-edu-api"

# ── Step 5: Update ECS service to the latest revision ──────
echo "==> Updating ECS service..."
aws ecs update-service \
  --cluster ${ECS_CLUSTER} \
  --service ${ECS_SERVICE} \
  --task-definition uni-edu-api \
  --force-new-deployment \
  --region ${AWS_REGION}

echo "==> Deploy triggered! Monitor at:"
echo "    https://${AWS_REGION}.console.aws.amazon.com/ecs/v2/clusters/${ECS_CLUSTER}/services/${ECS_SERVICE}"
