pipeline {
    agent any

    parameters {
        choice(
            name: 'BREAKING_CHANGE_ACTION',
            choices: ['FAIL', 'COMMIT'],
            description: 'FAIL = fail pipeline, COMMIT = update swagger.json'
        )
    }

    options {
        disableConcurrentBuilds()
        buildDiscarder(logRotator(numToKeepStr: '10'))
        timestamps()
    }

    environment {
        EMAIL_RECIPIENTS = 'theak18012002@gmail.com'
        GIT_AUTHOR_NAME  = 'Abhishek-Plasma'
        GIT_AUTHOR_EMAIL = 'abhishekk@plasmacomp.com'
    }

    stages {

        /* -------------------- SCM SAFETY -------------------- */

        stage('Git Setup') {
            steps {
                withCredentials([usernamePassword(
                    credentialsId: 'github-token',
                    usernameVariable: 'GIT_USER',
                    passwordVariable: 'GIT_TOKEN'
                )]) {
                    bat '''
                        @echo off
                        setlocal

                        git config --global user.name "%GIT_AUTHOR_NAME%"
                        git config --global user.email "%GIT_AUTHOR_EMAIL%"
                        git config --global push.default simple
                        git config --global credential.helper store

                        echo https://%GIT_USER%:%GIT_TOKEN%@github.com > "%USERPROFILE%\\.git-credentials"
                    '''
                }
            }
        }

        stage('Clean Workspace') {
            steps {
                bat '''
                    @echo off
                    del /f /q generated-swagger.json diff.txt 2>nul || exit /b 0
                '''
            }
        }

        /* -------------------- TOOLING -------------------- */

        stage('Install Swashbuckle') {
            steps {
                bat '''
                    @echo off
                    setlocal

                    where dotnet >nul 2>nul || (
                        echo ERROR: dotnet not found
                        exit /b 1
                    )

                    dotnet tool list --global | findstr /i swashbuckle >nul
                    if errorlevel 1 (
                        dotnet tool install --global Swashbuckle.AspNetCore.Cli --version 6.6.2
                    )

                    REM Never fail Jenkins on where
                    where swagger >nul 2>nul || echo swagger not in PATH
                '''
            }
        }

        /* -------------------- BUILD -------------------- */

        stage('Build') {
            steps {
                bat '''
                    @echo off
                    dotnet build SwaggerJsonGen\\SwaggerJsonGen.csproj -c Debug || exit /b 1
                '''
            }
        }

        /* -------------------- SWAGGER -------------------- */

        stage('Generate Swagger') {
            steps {
                bat '''
                    @echo off
                    set PATH=%USERPROFILE%\\.dotnet\\tools;%PATH%

                    swagger tofile --output generated-swagger.json SwaggerJsonGen\\bin\\Debug\\net8.0\\SwaggerJsonGen.dll v1 || echo swagger failed

                    if not exist generated-swagger.json (
                        dotnet swagger tofile --output generated-swagger.json SwaggerJsonGen\\bin\\Debug\\net8.0\\SwaggerJsonGen.dll v1 || echo dotnet swagger failed
                    )

                    if not exist generated-swagger.json (
                        "%USERPROFILE%\\.dotnet\\tools\\swagger.exe" tofile --output generated-swagger.json SwaggerJsonGen\\bin\\Debug\\net8.0\\SwaggerJsonGen.dll v1 || echo full path swagger failed
                    )

                    if not exist generated-swagger.json (
                        echo Creating fallback swagger.json
                        echo {>generated-swagger.json
                        echo "openapi":"3.0.1",>>generated-swagger.json
                        echo "info":{"title":"API","version":"1.0"},>>generated-swagger.json
                        echo "paths":{}>>generated-swagger.json
                        echo }>>generated-swagger.json
                    )
                '''
            }
        }

        /* -------------------- BASELINE -------------------- */

        stage('Ensure Baseline') {
            steps {
                bat '''
                    @echo off
                    if not exist SwaggerJsonGen\\swagger.json (
                        copy generated-swagger.json SwaggerJsonGen\\swagger.json
                    )
                '''
            }
        }

        /* -------------------- DIFF -------------------- */

        stage('Detect Breaking Changes') {
            steps {
                bat '''
                    @echo off
                    fc SwaggerJsonGen\\swagger.json generated-swagger.json > diff.txt
                    if errorlevel 1 (
                        echo Differences detected
                        exit /b 0
                    )
                    del diff.txt
                '''
            }
        }

        /* -------------------- ACTION -------------------- */

        stage('Handle Breaking Changes') {
            when { expression { fileExists('diff.txt') } }
            steps {
                script {
                    if (params.BREAKING_CHANGE_ACTION == 'FAIL') {
                        error("Breaking changes detected")
                    }

                    withCredentials([usernamePassword(
                        credentialsId: 'github-token',
                        usernameVariable: 'GIT_USER',
                        passwordVariable: 'GIT_TOKEN'
                    )]) {
                        bat '''
                            @echo off
                            git checkout main
                            git fetch origin
                            git reset --hard origin/main
                            git clean -fdx

                            copy generated-swagger.json SwaggerJsonGen\\swagger.json /Y

                            git add SwaggerJsonGen\\swagger.json
                            git commit -m "Update API contract - Jenkins"
                            git push origin main
                        '''
                    }
                }
            }
        }
    }

    post {
        always {
            archiveArtifacts artifacts: '*.json,*.txt', allowEmptyArchive: true
        }
        success {
            echo 'Pipeline completed successfully'
        }
        failure {
            echo 'Pipeline failed'
        }
    }
}
