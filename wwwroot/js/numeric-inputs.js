(function () {
    const numericSelector = 'input[type="number"], input[inputmode="numeric"], input[data-numeric]';
    const editingKeys = new Set([
        'Backspace',
        'Delete',
        'Tab',
        'Escape',
        'Enter',
        'ArrowLeft',
        'ArrowRight',
        'ArrowUp',
        'ArrowDown',
        'Home',
        'End'
    ]);

    function allowsDecimal(input) {
        const mode = input.dataset.numeric;

        if (mode === 'integer') return false;
        if (mode === 'decimal') return true;
        if (input.getAttribute('inputmode') === 'numeric' && input.type !== 'number') return false;

        const step = input.getAttribute('step');
        return step === 'any' || (step && step !== '1');
    }

    function cleanNumber(value, allowDecimal) {
        let cleaned = '';
        let hasDecimal = false;

        for (const char of value) {
            if (char >= '0' && char <= '9') {
                cleaned += char;
                continue;
            }

            if (allowDecimal && char === '.' && !hasDecimal) {
                cleaned += char;
                hasDecimal = true;
            }
        }

        return cleaned;
    }

    function isAllowedText(input, text) {
        return cleanNumber(text, allowsDecimal(input)) === text;
    }

    function getNumericInput(target) {
        if (!(target instanceof Element)) return null;
        return target.closest(numericSelector);
    }

    function getSelection(input) {
        try {
            return {
                start: input.selectionStart ?? input.value.length,
                end: input.selectionEnd ?? input.value.length
            };
        } catch {
            return {
                start: input.value.length,
                end: input.value.length
            };
        }
    }

    function replaceSelection(input, text) {
        const selection = getSelection(input);
        const start = selection.start;
        const end = selection.end;
        const nextValue = input.value.slice(0, start) + text + input.value.slice(end);
        const cleaned = cleanNumber(nextValue, allowsDecimal(input));

        input.value = cleaned;
        input.dispatchEvent(new Event('input', { bubbles: true }));
    }

    document.addEventListener('beforeinput', function (event) {
        const input = getNumericInput(event.target);
        if (!input || event.inputType !== 'insertText' || !event.data) return;

        if (!isAllowedText(input, event.data)) {
            event.preventDefault();
        }
    });

    document.addEventListener('keydown', function (event) {
        const input = getNumericInput(event.target);
        if (!input || event.ctrlKey || event.metaKey || event.altKey || editingKeys.has(event.key)) return;

        if (!isAllowedText(input, event.key)) {
            event.preventDefault();
        }
    });

    document.addEventListener('paste', function (event) {
        const input = getNumericInput(event.target);
        if (!input) return;

        const pastedText = event.clipboardData?.getData('text') || '';
        const cleaned = cleanNumber(pastedText, allowsDecimal(input));

        if (cleaned !== pastedText) {
            event.preventDefault();
            replaceSelection(input, cleaned);
        }
    });

    document.addEventListener('input', function (event) {
        const input = getNumericInput(event.target);
        if (!input) return;

        const cleaned = cleanNumber(input.value, allowsDecimal(input));
        if (input.value !== cleaned) {
            input.value = cleaned;
        }
    });
})();
