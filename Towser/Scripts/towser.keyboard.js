var towserKeyboardInit = function (term) {
    var emitFunction = function (ch) { term.emit("data", ch); };

    /**
     * keymap object
     * index: keyCode
     * value: string _or_ tuple of [unmodifiedString, shiftString, ctrlString]
     */

    var keymap = {
        // backspace
        8: '\b',
        // tab
        9: ['\t', '\x02\x0f'],
        // return
        13: '\r\n',
        // escape
        27: '\x1b',
        // page up
        33: ['\x1b[5~', '\x02 '],
        // page down
        34: ['\x1b[6~', '\x02v'],
        // end
        35: ['\x1b[4~', null, '\x02u'],
        // home
        36: '\x1b[1~',
        // left
        37: ['\x1b[D', null, '\x02s'],
        // up
        38: '\x1b[A',
        // right
        39: ['\x1b[C', null, '\x02t'],
        // down
        40: '\x1b[B',
        // insert
        45: ['\x1b[2~', null, '\x02.'],
        // delete
        46: ['\x1b[3~', null, '\x02/'],
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

    // handle keydown and keypress events
    var onKeyHandler = function (event) {
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
                var km = keymap[kc];
                if (typeof km === "string") {
                    ch = km;
                } else if (km instanceof Array) {
                    if (event.shiftKey) {
                        ch = km[1];
                    } else if (event.ctrlKey) {
                        ch = km[2];
                    } else {
                        ch = km[0];
                    }
                }
                
                if (ch === undefined) { return true; }
            }
        }

        if (ch) { emitFunction(ch); }

        event.preventDefault();
        return false;
    };

    term.keyDown = onKeyHandler;
    term.keyPress = onKeyHandler;
};