### This file takes a built and zipped VSO, adds a deploy to rhel7 docker file, and uploads it to a nexus repo, the deploys it to openshift. 

echo oc_nexus_credentials=$oc_nexus_credentials
echo oc_openshift_credentials=$oc_openshift_credentials
echo oc_dfe_deploy_url=$oc_dfe_deploy_url
echo oc_nexus_repo=$oc_nexus_repo
echo oc_project=$oc_project
echo oc_app=$oc_app

echo BUILD_ARTIFACTSTAGINGDIRECTORY=$BUILD_ARTIFACTSTAGINGDIRECTORY
echo BUILD_BINARIESDIRECTORY=$BUILD_BINARIESDIRECTORY
echo BUILD_BUILDID=$BUILD_BUILDID

cd $BUILD_ARTIFACTSTAGINGDIRECTORY
INNER_ZIP=`find . -name \*.zip`

# Create the docker file
cat > Dockerfile <<EOF
FROM registry.access.redhat.com/dotnet/dotnet-20-runtime-rhel7

ADD . .

CMD ["dotnet", "govukblank.dll"]
EOF

echo ### ADDING Dockerfile TO $INNER_ZIP
zip $INNER_ZIP Dockerfile
mv $INNER_ZIP $BUILD_BUILDID.zip

#echo ### PUBLISHING $INNER_ZIP TO $oc_nexus_repo
#curl -k -v -u $oc_nexus_credentials --upload-file $BUILD_BUILDID.zip $oc_nexus_repo

# Download oc
#curl -u $oc_nexus_credentials -O -k https://nexus.demo.dfe.secnix.co.uk/repository/dfe_admin/oc-3.6.173.0.49-linux.tar
#tar xfv oc-3.6.173.0.49-linux.tar
wget -q -O oc.tar https://www.dropbox.com/s/ir3xms1m72p5lsh/oc-3.6.173.0.49-linux.tar?dl=0
tar xfv oc.tar

echo ### LOGGING IN
./oc login --insecure-skip-tls-verify https://demo.dfe.secnix.co.uk:8443 --token="$oc_openshift_credentials"

echo ### RUNNIGN BUILD appname IN $OC_PROJECT WITH $BUILD_BUILDID.zip
./oc project $oc_project
./oc start-build $oc_app -n $oc_project --from-archive=$BUILD_BUILDID.zip
