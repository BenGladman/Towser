var towserPconInit = function () {
    var term = towserTermInit();

    var connection = $.connection('/towserPcon');

    // receive from signalR
    connection.received(function (data) {
        term.write(data);
    });

    connection.start()
        .done(function () {
            console.log('Now connected');
        })
        .fail(function () {
            console.log('Could not Connect!');
        });

    // receive from terminal
    term.ondata = function (data) { connection.send(data); }

    // reset terminal
    term.onreset = function () { connection.stop(); }
}