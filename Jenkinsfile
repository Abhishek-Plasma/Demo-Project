pipeline {
    agent any

    options {
        disableConcurrentBuilds()
        buildDiscarder(logRotator(numToKeepStr: '10'))
    }

    stages {

        stage('Build') {
            steps {
                bat 'dotnet build'
            }
        }

        stage('Generate Swagger') {
            steps {
                bat '''
                dotnet tool run swagger tofile ^
                --output generated-swagger.json ^
                bin\\Debug\\net8.0\\SwaggerJsonGen.dll v1
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
