var MyLibrary = {
    $xtringToUTF8: function(str, outPtr, maxBytesToWrite) {
        return stringToUTF8Array(str, HEAPU8, outPtr, maxBytesToWrite);
    },
    $XTF8ToString: function(ptr, maxBytesToRead) {
        return UTF8ToString(ptr, maxBytesToRead);
    },
    $legacyReadEmAsmArgs: function(sigPtr, buf) {
        var args = [];
        var ch;
        buf >>= 2;

        while ((ch = HEAPU8[sigPtr++])) {
            if (ch === 100 || ch === 102 || ch === 106) {
                buf += (buf & 1);
            }

            if (ch === 105 || ch === 112) {
                args.push(HEAP32[buf]);
                buf += 1;
                continue;
            }

            if (ch === 115) {
                args.push(UTF8ToString(HEAP32[buf]));
                buf += 1;
                continue;
            }

            if (ch === 100 || ch === 102) {
                args.push(HEAPF64[buf >> 1]);
                buf += 2;
                continue;
            }

            if (ch === 106) {
                if (typeof HEAP64 !== "undefined") {
                    args.push(HEAP64[buf >> 1]);
                } else {
                    var low = HEAP32[buf] >>> 0;
                    var high = HEAP32[buf + 1];
                    args.push(low + 4294967296 * high);
                }
                buf += 2;
                continue;
            }

            abort("Invalid EM_ASM signature character: " + String.fromCharCode(ch));
        }

        return args;
    },
    emscripten_asm_const_int__deps: ['$legacyReadEmAsmArgs'],
    emscripten_asm_const_int: function(code, sigPtr, argbuf) {
        var args = legacyReadEmAsmArgs(sigPtr, argbuf);
        if (!ASM_CONSTS.hasOwnProperty(code)) {
            abort("No EM_ASM constant found at address " + code);
        }
        return ASM_CONSTS[code].apply(null, args);
    },
    emscripten_asm_const_int_sync_on_main_thread__deps: ['$legacyReadEmAsmArgs'],
    emscripten_asm_const_int_sync_on_main_thread: function(code, sigPtr, argbuf) {
        var args = legacyReadEmAsmArgs(sigPtr, argbuf);
        if (!ASM_CONSTS.hasOwnProperty(code)) {
            abort("No EM_ASM constant found at address " + code);
        }
        return ASM_CONSTS[code].apply(null, args);
    },
    emscripten_asm_const_ptr_sync_on_main_thread__deps: ['$legacyReadEmAsmArgs'],
    emscripten_asm_const_ptr_sync_on_main_thread: function(code, sigPtr, argbuf) {
        var args = legacyReadEmAsmArgs(sigPtr, argbuf);
        if (!ASM_CONSTS.hasOwnProperty(code)) {
            abort("No EM_ASM constant found at address " + code);
        }
        return ASM_CONSTS[code].apply(null, args);
    }
};
mergeInto(LibraryManager.library, MyLibrary);
