pipeline {
    agent none

    options {
        timestamps()
        disableConcurrentBuilds()
    }

    stages {
        stage('Backend Build and Test') {
            agent {
                docker {
                    image 'mcr.microsoft.com/dotnet/sdk:8.0'
                    args '-u root'
                }
            }
            steps {
                sh 'dotnet restore backend/AutoPilot.Api/AutoPilot.Api.csproj'
                sh 'dotnet build backend/AutoPilot.Api/AutoPilot.Api.csproj --configuration Release --no-restore'
                sh 'dotnet test backend/AutoPilot.Api.Tests/AutoPilot.Api.Tests.csproj --configuration Release'
            }
        }

        stage('Frontend Lint Test Build') {
            agent {
                docker {
                    image 'node:20-alpine'
                    args '-u root'
                }
            }
            steps {
                dir('frontend') {
                    sh 'npm ci'
                    sh 'npm run lint'
                    sh 'npm run test'
                    sh 'npm run build'
                }
            }
        }
    }

    post {
        success {
            echo 'Pipeline succeeded: backend and frontend passed all checks.'
        }
        failure {
            echo 'Pipeline failed: check the stage logs above.'
        }
    }
}
