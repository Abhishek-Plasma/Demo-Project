pipeline {
    agent any

    options {
        disableConcurrentBuilds()
        buildDiscarder(logRotator(numToKeepStr: '10'))
    }

    stages {
        stage('Install Tools') {
            steps {
                bat '''
                    dotnet tool install --global Swashbuckle.AspNetCore.Cli --version 6.6.0
                    dotnet tool install --global dotnet-tool
                    dotnet new tool-manifest --force
                    dotnet tool install Swashbuckle.AspNetCore.Cli
                '''
            }
        }

        stage('Restore Tools') {
            steps {
                bat 'dotnet tool restore'
            }
        }

        stage('Build') {
            steps {
                // Disable the post-build event temporarily, or modify the .csproj
                bat 'dotnet build /p:PostBuildEvent='
                // OR: Build without the problematic post-build event
                // bat 'dotnet build --no-incremental'
            }
        }

        stage('Generate Swagger') {
            steps {
                bat '''
                    dotnet swagger tofile ^
                    --output generated-swagger.json ^
                    SwaggerJsonGen\\bin\\Debug\\net8.0\\SwaggerJsonGen.dll v1
                '''
            }
        }

        stage('Lint Swagger') {
            steps {
                bat '''
                    npx @stoplight/spectral-cli lint generated-swagger.json ^
                    -r spectral.yaml
                '''
            }
        }

        stage('Breaking Change Check') {
            steps {
                bat '''
                    "C:\\Program Files\\oasdiff\\oasdiff.exe" breaking ^
                    swagger.json generated-swagger.json
                '''
            }
        }
    }

    post {
        success {
            echo '✅ Swagger validation passed'
        }
        failure {
            echo '❌ Swagger validation failed (lint or breaking change)'
        }
    }
}
