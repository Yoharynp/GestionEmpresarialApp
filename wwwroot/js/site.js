// Normalize text for accent- and case-insensitive comparison
function normalizeText(s) {
    return (s || '').normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase();
}

// Wire real-time client-side search on a table (no page reload).
// inputId   — id of the search <input>
// tableId   — id of the <table>
// countId   — id of the counter element (optional)
// countWord — label word, e.g. 'clientes'
function wireTableSearch(inputId, tableId, countId, countWord) {
    var input = document.getElementById(inputId);
    var tbody = document.querySelector('#' + tableId + ' tbody');
    if (!input || !tbody) return;

    function doFilter() {
        var term = normalizeText(input.value.trim());
        var rows = Array.prototype.slice.call(tbody.querySelectorAll('tr'));
        var visible = 0;
        rows.forEach(function (row) {
            // empty-state row (single spanning cell) — always show when term is empty
            if (row.cells.length <= 1) {
                row.style.display = term ? 'none' : '';
                return;
            }
            var text = normalizeText(row.textContent);
            var show = !term || text.indexOf(term) !== -1;
            row.style.display = show ? '' : 'none';
            if (show) visible++;
        });
        // update empty-state row visibility
        rows.forEach(function (row) {
            if (row.cells.length <= 1) row.style.display = (visible === 0 ? '' : 'none');
        });
        if (countId) {
            var el = document.getElementById(countId);
            if (el) el.textContent = 'Mostrando ' + visible + ' ' + (countWord || 'registros');
        }
    }

    input.addEventListener('input', doFilter);
    // Prevent Enter from submitting any parent form
    input.addEventListener('keydown', function (e) {
        if (e.key === 'Enter') e.preventDefault();
    });
}

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
