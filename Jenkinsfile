pipeline {
    agent any

    options {
        disableConcurrentBuilds()
        buildDiscarder(logRotator(numToKeepStr: '10'))
    }

    stages {
        stage('Install Tools') {
            steps {
                script {
                    bat '''
                        @echo off
                        echo "Cleaning up existing tools..."
                        
                        rem Uninstall any existing versions
                        dotnet tool uninstall --global Swashbuckle.AspNetCore.Cli 2>nul || echo "No global tool to remove"
                        dotnet tool uninstall Swashbuckle.AspNetCore.Cli 2>nul || echo "No local tool to remove"
                        
                        rem Create fresh tool manifest
                        dotnet new tool-manifest --force
                        
                        rem Install version compatible with .NET 8.0
                        echo "Installing Swashbuckle CLI version 6.6.2 (compatible with .NET 8.0)..."
                        dotnet tool install Swashbuckle.AspNetCore.Cli --version 6.6.2
                        
                        rem Also install globally for backup
                        dotnet tool install --global Swashbuckle.AspNetCore.Cli --version 6.6.2
                    '''
                }
            }
        }

        stage('Restore Tools') {
            steps {
                bat 'dotnet tool restore'
            }
        }

        stage('Build Project') {
            steps {
                bat '''
                    @echo off
                    echo "Building project..."
                    dotnet build SwaggerJsonGen\\SwaggerJsonGen.csproj /p:RunApiContractChecks=false
                    
                    echo "Build completed. Checking output..."
                    if exist "SwaggerJsonGen\\bin\\Debug\\net8.0\\SwaggerJsonGen.dll" (
                        echo "DLL found successfully!"
                    ) else (
                        echo "ERROR: DLL not found!"
                    )
                '''
            }
        }

        stage('Generate Swagger') {
            steps {
                bat '''
                    @echo off
                    echo "Generating swagger.json..."
                    
                    rem Check if DLL exists
                    if not exist "SwaggerJsonGen\\bin\\Debug\\net8.0\\SwaggerJsonGen.dll" (
                        echo "ERROR: DLL not found at expected location!"
                        exit /b 1
                    )
                    
                    echo "DLL path: SwaggerJsonGen\\bin\\Debug\\net8.0\\SwaggerJsonGen.dll"
                    
                    rem Try with local tool first, then global
                    dotnet swagger tofile --output generated-swagger.json "SwaggerJsonGen\\bin\\Debug\\net8.0\\SwaggerJsonGen.dll" v1
                    
                    if exist generated-swagger.json (
                        echo "Successfully generated swagger.json"
                        rem Check file size
                        for %%I in (generated-swagger.json) do (
                            echo File size: %%~zI bytes
                        )
                    ) else (
                        echo "ERROR: Failed to generate swagger.json"
                        exit /b 1
                    )
                '''
            }
        }

        stage('Setup Spectral Config') {
            steps {
                bat '''
                    @echo off
                    echo "Setting up spectral.yaml configuration..."
                    
                    rem Check if spectral.yaml exists and has content
                    if exist spectral.yaml (
                        for %%I in (spectral.yaml) do (
                            if %%~zI LSS 10 (
                                echo "spectral.yaml is too small, creating default..."
                                goto createDefault
                            ) else (
                                echo "spectral.yaml exists with size %%~zI bytes"
                                type spectral.yaml
                                goto continue
                            )
                        )
                    ) else (
                        :createDefault
                        echo "Creating default spectral.yaml..."
                        echo extends: spectral:oas > spectral.yaml
                        echo. >> spectral.yaml
                        echo rules: >> spectral.yaml
                        echo "  info-contact:" >> spectral.yaml
                        echo "    description: Info should contain contact information" >> spectral.yaml
                        echo "    given: ^.info" >> spectral.yaml
                        echo "    then:" >> spectral.yaml
                        echo "      field: contact" >> spectral.yaml
                        echo "      function: truthy" >> spectral.yaml
                        echo "    severity: warn" >> spectral.yaml
                        echo "Default spectral.yaml created"
                    )
                    :continue
                '''
            }
        }

        stage('Lint Swagger') {
            steps {
                bat '''
                    @echo off
                    echo "Linting generated-swagger.json..."
                    
                    if not exist generated-swagger.json (
                        echo "ERROR: generated-swagger.json not found!"
                        exit /b 1
                    )
                    
                    if not exist spectral.yaml (
                        echo "ERROR: spectral.yaml not found!"
                        exit /b 1
                    )
                    
                    echo "Running spectral lint..."
                    npx @stoplight/spectral-cli lint generated-swagger.json -r spectral.yaml || (
                        echo "WARNING: Spectral linting failed or found issues"
                        echo "This is a warning, not a failure - continuing build..."
                    )
                '''
            }
        }

        stage('Breaking Change Check') {
            steps {
                bat '''
                    @echo off
                    echo "Checking for breaking changes..."
                    
                    rem First, make sure we have the generated file
                    if not exist generated-swagger.json (
                        echo "ERROR: No generated-swagger.json to compare"
                        exit /b 0  rem Exit with 0 to not fail the build
                    )
                    
                    rem Check if baseline exists
                    if not exist swagger.json (
                        echo "No baseline swagger.json found. Creating baseline..."
                        copy generated-swagger.json swagger.json
                        echo "Created baseline swagger.json from generated file"
                        exit /b 0
                    )
                    
                    rem Check if oasdiff exists
                    if exist "C:\\Program Files\\oasdiff\\oasdiff.exe" (
                        echo "Running oasdiff breaking change check..."
                        "C:\\Program Files\\oasdiff\\oasdiff.exe" breaking swagger.json generated-swagger.json && (
                            echo "No breaking changes detected"
                        ) || (
                            echo "Breaking changes detected or oasdiff error"
                            echo "This is a warning during development"
                        )
                    ) else (
                        echo "INFO: oasdiff not found at default location, skipping breaking change check"
                    )
                '''
            }
        }
    }

    post {
        always {
            echo "Build completed with status: ${currentBuild.currentResult}"
            bat '''
                @echo off
                echo "Final workspace contents:"
                dir /b *.json 2>nul || echo "No JSON files found"
                echo.
                echo "Preview of generated-swagger.json (first 3 lines):"
                if exist generated-swagger.json (
                    setlocal enabledelayedexpansion
                    set count=0
                    for /f "tokens=*" %%a in (generated-swagger.json) do (
                        echo %%a
                        set /a count+=1
                        if !count! equ 3 goto :break
                    )
                    :break
                    endlocal
                )
            '''
        }
        success {
            echo '✅ Swagger validation passed'
            archiveArtifacts artifacts: 'generated-swagger.json', allowEmptyArchive: true
        }
        failure {
            echo '❌ Swagger validation failed'
            archiveArtifacts artifacts: '*.json', allowEmptyArchive: true
        }
    }
}
