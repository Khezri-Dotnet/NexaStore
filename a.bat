$file = ".\tests\NexaStore.Application.Tests\AuthenticationRefreshTests.cs"

$content = Get-Content $file -Raw

$pattern = '(?s)    private AuthenticationController CreateSut\(\)\s*\{.*?    \}'

$replacement = @'
    private AuthenticationController CreateSut()
    {
        var registerHandler =
            new NexaStore.Application.Features.Authentication.Register.RegisterHandler(
                _identityService.Object);

        var loginHandler =
            new NexaStore.Application.Features.Authentication.Login.LoginHandler(
                _identityService.Object,
                _jwtTokenGenerator.Object,
                _refreshTokenService.Object);

        return new AuthenticationController(
            registerHandler,
            loginHandler,
            _refreshTokenService.Object,
            _identityService.Object,
            _jwtTokenGenerator.Object);
    }
'@

$content = [regex]::Replace($content, $pattern, $replacement, 1)

Set-Content $file $content -Encoding utf8