pipeline {
    agent any

    options {
        disableConcurrentBuilds()
        buildDiscarder(logRotator(numToKeepStr: '10'))
    }

    stages {
        stage('Clean Workspace') {
            steps {
                bat '''
                    @echo off
                    echo "Cleaning up..."
                    if exist generated-swagger.json del generated-swagger.json
                    if exist diff.txt del diff.txt
                '''
            }
        }

        stage('Install Tools') {
            steps {
                bat '''
                    @echo off
                    echo "Installing Swashbuckle CLI..."
                    dotnet tool install --global Swashbuckle.AspNetCore.Cli --version 6.6.2
                '''
            }
        }

        stage('Build') {
            steps {
                bat '''
                    @echo off
                    echo "Building project..."
                    dotnet build SwaggerJsonGen\\SwaggerJsonGen.csproj
                '''
            }
        }

        stage('Generate Swagger') {
            steps {
                bat '''
                    @echo off
                    echo "Generating swagger.json..."
                    
                    rem Try dotnet swagger first, then regular swagger
                    dotnet swagger tofile --output generated-swagger.json SwaggerJsonGen\\bin\\Debug\\net8.0\\SwaggerJsonGen.dll v1
                    
                    if errorlevel 1 (
                        echo "dotnet swagger failed, trying swagger command..."
                        swagger tofile --output generated-swagger.json SwaggerJsonGen\\bin\\Debug\\net8.0\\SwaggerJsonGen.dll v1
                    )
                    
                    if exist generated-swagger.json (
                        echo "SUCCESS: Swagger file generated"
                        for %%I in (generated-swagger.json) do echo File size: %%~zI bytes
                    ) else (
                        echo "ERROR: Failed to generate swagger"
                        exit /b 1
                    )
                '''
            }
        }

        stage('Check Breaking Changes') {
            steps {
                script {
                    echo "=== CHECKING FOR BREAKING CHANGES ==="
                    
                    bat '''
                        @echo off
                        echo "Step 1: Verify files exist..."
                        if not exist generated-swagger.json (
                            echo "ERROR: No generated-swagger.json found"
                            exit /b 1
                        )
                        
                        if not exist swagger.json (
                            echo "INFO: No baseline found. Creating baseline..."
                            copy generated-swagger.json swagger.json
                            echo "Baseline created from current version."
                            exit /b 0
                        )
                        
                        echo "Step 2: Compare file sizes..."
                        for %%I in (swagger.json) do set baseline=%%~zI
                        for %%I in (generated-swagger.json) do set current=%%~zI
                        echo Baseline: !baseline! bytes
                        echo Current:  !current! bytes
                        
                        if !baseline! == !current! (
                            echo "File sizes match."
                        ) else (
                            echo "WARNING: File sizes differ!"
                        )
                        
                        echo.
                        echo "Step 3: Simple text comparison..."
                        fc swagger.json generated-swagger.json > diff.txt
                        
                        if errorlevel 1 (
                            echo "DIFFERENCES FOUND:"
                            echo "=================="
                            type diff.txt
                            echo "=================="
                            echo.
                            echo "BREAKING CHANGE ALERT: API has changed!"
                            echo "Review the differences above."
                        ) else (
                            echo "No differences found. API is stable."
                            if exist diff.txt del diff.txt
                        )
                    '''
                    
                    // Try oasdiff if available
                    bat '''
                        @echo off
                        echo.
                        echo "Step 4: Checking for oasdiff tool..."
                        
                        if exist "C:\\Program Files\\oasdiff\\oasdiff.exe" (
                            echo "Found oasdiff, running detailed analysis..."
                            "C:\\Program Files\\oasdiff\\oasdiff.exe" breaking swagger.json generated-swagger.json
                        ) else (
                            echo "oasdiff not found. Install it for detailed breaking change analysis."
                            echo "Download from: https://github.com/Tufin/oasdiff"
                        )
                    '''
                }
            }
        }
    }

    post {
        always {
            echo "=== BUILD COMPLETED ==="
            echo "Status: ${currentBuild.currentResult}"
            
            bat '''
                @echo off
                echo.
                echo "Files in workspace:"
                dir /b *.json 2>nul || echo "No JSON files"
                
                if exist diff.txt (
                    echo.
                    echo "Diff file exists. Review it for changes."
                )
            '''
            
            // Archive everything for review
            archiveArtifacts artifacts: '*.json, *.txt', allowEmptyArchive: true
        }
        success {
            echo '✅ Pipeline completed successfully'
        }
        failure {
            echo '❌ Pipeline failed'
        }
    }
}
