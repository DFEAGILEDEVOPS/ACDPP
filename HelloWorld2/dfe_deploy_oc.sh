# Capture args
oc_nexus_credentials=$1
oc_openshift_credentials=$2
oc_dfe_deploy_url=$3
oc_nexus_repo=$4
oc_project=$5
oc_app=$6
# Print args
echo oc_nexus_credentials=$oc_nexus_credentials
echo oc_openshift_credentials=$oc_openshift_credentials 
echo oc_dfe_deploy_url=$oc_dfe_deploy_url
echo oc_nexus_repo=$oc_nexus_repo
echo oc_project=$oc_project
echo oc_app=$oc_app
# Print build env vars
echo BUILD_ARTIFACTSTAGINGDIRECTORY=$BUILD_ARTIFACTSTAGINGDIRECTORY
echo BUILD_BINARIESDIRECTORY=$BUILD_BINARIESDIRECTORY
echo BUILD_BUILDID=$BUILD_BUILDID
# Export args so that the next script knows them
export oc_nexus_credentials
export oc_openshift_credentials
export oc_dfe_deploy_url
export oc_nexus_repo
export oc_project
export oc_app
# Grab the script from the server that does the actual deploy
#echo curl -u $oc_nexus_credentials -ksSf $oc_dfe_deploy_url | sh
#curl -u $oc_nexus_credentials -ksSf $oc_dfe_deploy_url | sh
./dotnet-20-runtime-rhel7.sh
