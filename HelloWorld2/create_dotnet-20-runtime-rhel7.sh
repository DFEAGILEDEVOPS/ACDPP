#!/bin/bash
oc_project_name=$1
echo oc_project_name=$oc_project_name
oc_build_config_name=$2
echo oc_build_config_name=$oc_build_config_name

./oc new-project $oc_project_name
./oc new-app --name=$oc_build_config_name dotnet:2.0~https://github.com/DFEAGILEDEVOPS/govuk-blank.git
./oc create route edge --service=$oc_build_config_name --hostname=${oc_build_config_name}-${oc_project_name}.demo.dfe.secnix.co.uk
./oc delete bc $oc_build_config_name -n $oc_project_name
./oc create -f - <<EOF 
apiVersion: v1
kind: BuildConfig
metadata:
  name: ${oc_build_config_name}
  namespace: ${oc_project_name}
  labels:
    app: ${oc_build_config_name}
    component: development
    logging-infra: development
    provider: openshift
spec:
  triggers:
    - type: ConfigChange
  runPolicy: Serial
  source:
    type: Binary
    binary: {}
  strategy:
    type: Docker
    dockerStrategy:
      from:
        kind: DockerImage
        name: dotnet/dotnet-20-runtime-rhel7
      dockerfilePath: Dockerfile
  output:
    to:
      kind: ImageStreamTag
      name: '${oc_build_config_name}:latest'
  resources: {}
  postCommit: {}
  nodeSelector: null
status:
  lastVersion: 25
EOF



