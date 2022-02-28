# Start an HTTP server
$uri = "http://localhost:8000/"
$separator = "--------------------------------------------------------------------"
$httpServer = [System.Net.HttpListener]::new()
$httpServer.Prefixes.Add($uri)
$httpServer.Start()

write-Host "HTTP server listening on $uri"
Write-Host $separator

try {
    while ($httpServer.IsListening) {
        # allow ctrl+c interrupt using the AsyncWaitHandle
        $contextAsync = $httpServer.GetContextAsync()
        while (-not $contextAsync.AsyncWaitHandle.WaitOne(100)) { }

        # current request info
        $context = $contextAsync.GetAwaiter().GetResult()

        # print the request to stdout
        write-Host "$($context.Request.HttpMethod) $($context.Request.Url)"
        if ($context.Request.HttpMethod -eq 'POST') {
            $payload = [System.IO.StreamReader]::new($context.Request.InputStream).ReadToEnd()
            if ($context.Request.Url.ToString().Contains("minidump")) {
                $payload = "minidump payload length: $($payload.Length)`n"
            }
            else {
            }
            Write-Host -NoNewline $payload
        }
        Write-Host $separator

        # send an empty response
        $context.Response.ContentLength64 = 0
        $context.Response.OutputStream.Close()
        if ($context.Request.Url.PathAndQuery -eq "/STOP") {
            break
        }
    }
}
finally {
    # This is always called when ctrl+c is used - note, this doesn't seem to be 100 % working...
    # instead, you can send a GET request to http://localhost:8000/STOP
    write-Host "HTTP server stopping!"
    $httpServer.Stop()
}
