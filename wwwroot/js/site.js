// Formats a local RD 10-digit block as (XXX)-XXX-XXXX
function formatLocalBlock(digits) {
    var d = digits.substring(0, 10);
    if (d.length <= 3)  return '(' + d;
    if (d.length <= 6)  return '(' + d.substring(0,3) + ')-' + d.substring(3);
    return '(' + d.substring(0,3) + ')-' + d.substring(3,6) + '-' + d.substring(6);
}

// Formats a phone number following RD conventions:
//   - starts with 8        → local RD: (809)-XXX-XXXX
//   - starts with 1 + 10d  → +1 (809)-XXX-XXXX
//   - starts with other CC → +CC (XXX)-XXX-XXXX  (if last 10 starts with 8)
//   - short non-8 prefix   → +CC (typing in progress)
function formatPhone(raw) {
    var digits = raw.replace(/\D/g, '');
    if (!digits) return '';

    if (digits[0] === '8') {
        return formatLocalBlock(digits);
    }

    if (digits.length > 10) {
        var ccLen = digits.length - 10;
        var cc    = digits.substring(0, ccLen);
        var local = digits.substring(ccLen);
        return '+' + cc + ' ' + formatLocalBlock(local);
    }

    // Country code still being typed
    return '+' + digits;
}

// Attach formatter to all inputs with data-phone attribute
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('input[data-phone]').forEach(function (input) {
        input.addEventListener('input', function () {
            var pos    = this.selectionStart;
            var before = this.value.length;
            this.value = formatPhone(this.value);
            var delta  = this.value.length - before;
            this.setSelectionRange(pos + delta, pos + delta);
        });

        input.addEventListener('keydown', function (e) {
            var allowed = ['Backspace','Delete','ArrowLeft','ArrowRight','ArrowUp','ArrowDown','Tab','Home','End'];
            if (allowed.indexOf(e.key) !== -1) return;
            if (e.ctrlKey || e.metaKey) return;
            if (!/^\d$/.test(e.key)) e.preventDefault();
        });

        var form = input.closest('form');
        if (form) {
            form.addEventListener('submit', function () {
                input.value = input.value.replace(/\D/g, '');
            }, true);
        }
    });
});
