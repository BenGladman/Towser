var init = function () {
    var vt100 = new VT100(132, 24, 'terminal');

    var connection = $.connection('/telnet');

    // receive from signalR
    connection.received(function (data) {
        console.log("receive " + data);
        vt100.write(data);
    });

    var keymap = {
        // backspace
        8: '\b',
        // tab
        9: '\t',
        // return
        13: '\r\n',
        // escape
        27: '\x1b',
        // page up
        33: '\x1b[5~',
        // page down
        34: '\x1b[6~',
        // end
        35: '\x1b[4~',
        // home
        36: '\x1b[1~',
        // down
        39: '\x1b[C',
        // left
        37: '\x1b[D',
        // up
        38: '\x1b[A',
        // right
        40: '\x1b[B',
        // insert
        45: '\x1b[2~',
        // delete
        46: '\x1b[3~',
        // f1
        112: '\x1bOP',
        // f2
        113: '\x1bOQ',
        // f3
        114: '\x1bOR',
        // f4
        115: '\x1bOS',
        // f5 (send f14 sequence)
        116: '\x1b[26~',
        // f6
        117: '\x1b[17~',
        // f7
        118: '\x1b[18~',
        // f8
        119: '\x1b[19~',
        // f9
        120: '\x1b[20~',
        // f10
        121: '\x1b[21~',

        // don't intercept f11 and f12 - let the browser use them
        // f11
        //122: '\x1b[22~',
        // f12
        //123: '\x1b[23~',
    };

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
                ch = keymap[kc];
                if (ch === undefined) { return true; }
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