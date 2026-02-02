pipeline {
    agent any

    options {
        disableConcurrentBuilds()
        buildDiscarder(logRotator(numToKeepStr: '10'))
    }

    environment {
        // Email configuration
        EMAIL_RECIPIENTS = 'abhishek@example.com'  // Change this to your email
        BUILD_URL = "${env.BUILD_URL}"
        JOB_NAME = "${env.JOB_NAME}"
        BUILD_NUMBER = "${env.BUILD_NUMBER}"
    }

    stages {
        stage('Clean Workspace') {
            steps {
                bat '''
                    @echo off
                    echo "Cleaning up..."
                    if exist generated-swagger.json del generated-swagger.json
                    if exist diff.txt del diff.txt
                    if exist breaking_changes_report.txt del breaking_changes_report.txt
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
                    
                    dotnet swagger tofile --output generated-swagger.json SwaggerJsonGen\\bin\\Debug\\net8.0\\SwaggerJsonGen.dll v1
                    
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
                        
                        echo "Step 2: Create breaking changes report..."
                        echo "=== API BREAKING CHANGES REPORT ===" > breaking_changes_report.txt
                        echo "Build: %JOB_NAME% #%BUILD_NUMBER%" >> breaking_changes_report.txt
                        echo "Build URL: %BUILD_URL%" >> breaking_changes_report.txt
                        echo "Generated at: %DATE% %TIME%" >> breaking_changes_report.txt
                        echo. >> breaking_changes_report.txt
                        
                        echo "Step 3: Compare file sizes..."
                        for %%I in (swagger.json) do set baseline=%%~zI
                        for %%I in (generated-swagger.json) do set current=%%~zI
                        echo Baseline: !baseline! bytes >> breaking_changes_report.txt
                        echo Current:  !current! bytes >> breaking_changes_report.txt
                        echo. >> breaking_changes_report.txt
                        
                        if !baseline! == !current! (
                            echo "File sizes match." >> breaking_changes_report.txt
                        ) else (
                            echo "WARNING: File sizes differ!" >> breaking_changes_report.txt
                        )
                        
                        echo. >> breaking_changes_report.txt
                        echo "Step 4: Simple text comparison..." >> breaking_changes_report.txt
                        fc swagger.json generated-swagger.json > diff.txt
                        
                        if errorlevel 1 (
                            echo "DIFFERENCES FOUND:" >> breaking_changes_report.txt
                            echo "==================" >> breaking_changes_report.txt
                            type diff.txt >> breaking_changes_report.txt
                            echo "==================" >> breaking_changes_report.txt
                            echo. >> breaking_changes_report.txt
                            echo "BREAKING CHANGE ALERT: API has changed!" >> breaking_changes_report.txt
                            
                            rem Also display on console
                            type breaking_changes_report.txt
                            
                            echo "FAILING THE BUILD - Breaking changes detected!"
                            exit /b 1
                        ) else (
                            echo "No differences found. API is stable." >> breaking_changes_report.txt
                            if exist diff.txt del diff.txt
                        )
                    '''
                    
                    // Try oasdiff if available
                    bat '''
                        @echo off
                        echo.
                        echo "Step 5: Checking for oasdiff tool..." >> breaking_changes_report.txt
                        
                        if exist "C:\\Program Files\\oasdiff\\oasdiff.exe" (
                            echo "Found oasdiff, running detailed analysis..." >> breaking_changes_report.txt
                            "C:\\Program Files\\oasdiff\\oasdiff.exe" breaking swagger.json generated-swagger.json >> breaking_changes_report.txt 2>&1
                            
                            if errorlevel 1 (
                                echo "FAILING THE BUILD - Breaking changes detected by oasdiff!" >> breaking_changes_report.txt
                                exit /b 1
                            ) else (
                                echo "No breaking changes detected by oasdiff." >> breaking_changes_report.txt
                            )
                        ) else (
                            echo "oasdiff not found. Install it for detailed breaking change analysis." >> breaking_changes_report.txt
                            echo "Download from: https://github.com/Tufin/oasdiff" >> breaking_changes_report.txt
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
                dir /b *.json *.txt 2>nul || echo "No JSON or TXT files"
            '''
            
            // Archive everything for review
            archiveArtifacts artifacts: '*.json, *.txt', allowEmptyArchive: true
        }
        success {
            echo '✅ Pipeline completed successfully - No breaking changes'
            
            // Send success email (optional)
            emailext (
                subject: "✅ SUCCESS: API Validation Passed - ${env.JOB_NAME} #${env.BUILD_NUMBER}",
                body: """
                    <h2>API Validation Successful</h2>
                    <p>The API validation pipeline has completed successfully with no breaking changes detected.</p>
                    <p><b>Job:</b> ${env.JOB_NAME}</p>
                    <p><b>Build:</b> #${env.BUILD_NUMBER}</p>
                    <p><b>Build URL:</b> <a href="${env.BUILD_URL}">${env.BUILD_URL}</a></p>
                    <p><b>Status:</b> ${currentBuild.currentResult}</p>
                    <p>The generated OpenAPI specification is available in the build artifacts.</p>
                """,
                to: "${env.EMAIL_RECIPIENTS}",
                mimeType: 'text/html'
            )
        }
        failure {
            echo '❌ Pipeline failed - Breaking changes detected'
            
            // Read breaking changes report for email
            script {
                def breakingChangesReport = ""
                try {
                    breakingChangesReport = readFile('breaking_changes_report.txt')
                } catch (Exception e) {
                    breakingChangesReport = "Breaking changes report not available. Check build logs for details."
                }
                
                // Send failure email with breaking changes details
                emailext (
                    subject: "❌ FAILURE: API Breaking Changes Detected - ${env.JOB_NAME} #${env.BUILD_NUMBER}",
                    body: """
                        <h2>🚨 API Breaking Changes Detected</h2>
                        <p>The API validation pipeline has failed due to breaking changes in the API specification.</p>
                        
                        <p><b>Job:</b> ${env.JOB_NAME}</p>
                        <p><b>Build:</b> #${env.BUILD_NUMBER}</p>
                        <p><b>Build URL:</b> <a href="${env.BUILD_URL}">${env.BUILD_URL}</a></p>
                        <p><b>Status:</b> ${currentBuild.currentResult}</p>
                        
                        <h3>Breaking Changes Report:</h3>
                        <pre>${breakingChangesReport}</pre>
                        
                        <h3>Required Action:</h3>
                        <ol>
                            <li>Review the breaking changes listed above</li>
                            <li>If changes are intentional, update the baseline by running:<br>
                                <code>copy generated-swagger.json swagger.json</code></li>
                            <li>If changes are not intentional, fix the API code</li>
                            <li>Re-run the pipeline</li>
                        </ol>
                        
                        <p>The detailed comparison files are available in the build artifacts.</p>
                    """,
                    to: "${env.EMAIL_RECIPIENTS}",
                    mimeType: 'text/html',
                    attachLog: true
                )
            }
        }
    }
}
