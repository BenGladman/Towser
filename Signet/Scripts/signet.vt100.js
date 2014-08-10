var init = function () {
    var vt100 = new VT100(80, 24, 'terminal');

    var connection = $.connection('/telnet');

    // receive from signalR
    connection.received(function (data) {
        console.log("receive " + data);
        vt100.write(data);
    });

    // send to signalR
    var onKeyHandler = function (event) {
        if (!vt100) { return true; }

        var ch;

        if (event.type === "keypress") {
            // printable characters
            var charcode = event.charCode;
            console.info("keypress " + charcode);

            if (charcode < 32) { return true; }
            if (charcode === 127) { return true; }
            if (charcode > 255) { return true; }
            ch = String.fromCharCode(charcode);

        } else if (event.type === "keydown") {
            var kc = event.keyCode;
            console.info("keydown " + kc);

            if (kc >= 65 && kc <= 90 && event.ctrlKey && !event.shiftKey) {
                // ctrl-key input
                ch = String.fromCharCode(kc - 64);

            } else {
                switch (kc) {
                    case 8:
                        // backspace
                        ch = '\b';
                        break;

                    case 9:
                        // tab
                        ch = '\t';
                        break;

                    case 13:
                        // return
                        ch = '\r\n';
                        break;

                    case 27:
                        // escape
                        ch = '\x1b';
                        break;

                    case 38:
                        // up
                        ch = '\x1b[A';
                        break;

                    case 40:
                        // down
                        ch = '\x1b[B';
                        break;

                    case 39:
                        // right
                        ch = '\x1b[C';
                        break;

                    case 37:
                        // left
                        ch = '\x1b[D';
                        break;

                    case 46:
                        // delete
                        ch = '\x1b[3~';
                        break;

                    case 36:
                        // home
                        ch = '\x1b[H';
                        break;

                    case 27:
                        // escape
                        ch = '\x1bc';
                        break;

                    default:
                        return true;
                }
            }
        }

        if (ch) {
            console.log("send " + ch);
            connection.send(ch)
        }

        event.preventDefault();
        return false;
    }

    connection.start().done(function () {
        vt100.noecho();
        window.addEventListener("keypress", onKeyHandler, false);
        window.addEventListener("keydown", onKeyHandler, false);
    });
}

$(init);