#!/bin/bash
# ============================================================
# UNI-EDU Backend — One-time AWS infrastructure setup
# ============================================================
# Tạo: ECR repo, ECS cluster, CloudWatch log group,
#       SSM parameters, RDS instructions
#
# Usage:
#   chmod +x aws/setup.sh
#   ./aws/setup.sh
#
# Prerequisites: AWS CLI v2 configured with admin permissions
# ============================================================

set -euo pipefail

AWS_REGION="ap-southeast-1"
ECR_REPO="uni-edu-api"
ECS_CLUSTER="uni-edu-cluster"
LOG_GROUP="/ecs/uni-edu-api"

echo "=========================================="
echo "  UNI-EDU AWS Setup (region: ${AWS_REGION})"
echo "=========================================="

# ── 1. ECR Repository ─────────────────────────────────────
echo ""
echo "==> [1/4] Creating ECR repository..."
aws ecr create-repository \
  --repository-name ${ECR_REPO} \
  --region ${AWS_REGION} \
  --image-scanning-configuration scanOnPush=true \
  2>/dev/null && echo "    Created: ${ECR_REPO}" \
  || echo "    Already exists: ${ECR_REPO}"

# ── 2. ECS Cluster ────────────────────────────────────────
echo ""
echo "==> [2/4] Creating ECS cluster..."
aws ecs create-cluster \
  --cluster-name ${ECS_CLUSTER} \
  --region ${AWS_REGION} \
  2>/dev/null && echo "    Created: ${ECS_CLUSTER}" \
  || echo "    Already exists: ${ECS_CLUSTER}"

# ── 3. CloudWatch Log Group ───────────────────────────────
echo ""
echo "==> [3/4] Creating CloudWatch log group..."
aws logs create-log-group \
  --log-group-name ${LOG_GROUP} \
  --region ${AWS_REGION} \
  2>/dev/null && echo "    Created: ${LOG_GROUP}" \
  || echo "    Already exists: ${LOG_GROUP}"

# ── 4. SSM Parameters (secrets) ───────────────────────────
echo ""
echo "==> [4/4] Storing secrets in SSM Parameter Store..."
echo "    Enter values for each parameter (or press Enter to skip):"

declare -A PARAMS=(
  ["POSTGRES_DB"]="uni_edu"
  ["POSTGRES_USER"]="postgres"
  ["POSTGRES_PASSWORD"]=""
  ["POSTGRES_HOST"]=""
  ["POSTGRES_PORT"]="5432"
  ["JWT_SecretKey"]=""
)

for key in "${!PARAMS[@]}"; do
  default="${PARAMS[$key]}"
  if [ -n "$default" ]; then
    read -p "    ${key} [${default}]: " value
    value="${value:-$default}"
  else
    read -p "    ${key}: " value
  fi

  if [ -n "$value" ]; then
    param_type="String"
    if [[ "$key" == "POSTGRES_PASSWORD" || "$key" == "JWT_SecretKey" ]]; then
      param_type="SecureString"
    fi

    aws ssm put-parameter \
      --name "/uni-edu/${key}" \
      --value "${value}" \
      --type "${param_type}" \
      --overwrite \
      --region ${AWS_REGION} \
      > /dev/null
    echo "    ✓ /uni-edu/${key}"
  else
    echo "    ✗ Skipped: ${key}"
  fi
done

echo ""
echo "=========================================="
echo "  Setup complete!"
echo "=========================================="
echo ""
echo "Next steps:"
echo "  1. Create RDS PostgreSQL (see instructions below)"
echo "  2. Update /uni-edu/POSTGRES_HOST with RDS endpoint"
echo "  3. Create ECS Task Definition:"
echo "     aws ecs register-task-definition --cli-input-json file://aws/task-definition.json --region ${AWS_REGION}"
echo "  4. Create ECS Service with ALB (see guide)"
echo "  5. Run: ./aws/deploy.sh"
echo ""
echo "── RDS PostgreSQL Setup (AWS Console) ──"
echo "  • Engine: PostgreSQL 15"
echo "  • Template: Free tier (db.t3.micro)"
echo "  • DB name: uni_edu"
echo "  • Master username/password: same as SSM params"
echo "  • VPC: same as ECS cluster"
echo "  • Public access: No"
echo "  • Security group: allow port 5432 from ECS security group"
echo ""
