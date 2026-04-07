/**
 * SV22T1020071.Shop — Global JS
 * - Intercept AddToCart form → AJAX POST
 * - Cart badge live update
 * - Toast notification
 */
$(document).ready(function () {

    // ── Intercept AddToCart forms → AJAX ─────────────────────────────────
    $(document).on('submit', 'form[action*="AddToCart"]', function (e) {
        e.preventDefault();
        var form      = $(this);
        var isBuyNow  = form.find('input[name="buyNow"]').length > 0;

        $.post(form.attr('action'), form.serialize(), function (res) {
            if (res.success) {
                // Update cart badge
                $('.cart-badge').text(res.totalQuantity);

                if (isBuyNow) {
                    window.location.href = '/Cart/Index';
                } else {
                    showToast('success', res.message);
                }
            } else {
                showToast('error', res.message);
            }
        }).fail(function () {
            showToast('error', 'Có lỗi xảy ra khi kết nối máy chủ.');
        });
    });

    // ── Load initial cart badge count ─────────────────────────────────────
    // Badge is already rendered server-side in _Layout; no extra call needed.

});

// ── Toast Notification ────────────────────────────────────────────────────
function showToast(type, message) {
    var iconMap = {
        success: '<i class="bi bi-check-circle-fill" style="color:#4ade80;"></i>',
        error:   '<i class="bi bi-exclamation-circle-fill" style="color:#f87171;"></i>',
        info:    '<i class="bi bi-info-circle-fill" style="color:#60a5fa;"></i>'
    };
    var icon = iconMap[type] || iconMap['info'];

    // Remove existing toasts
    $('.app-toast-wrapper').remove();

    var html = `
    <div class="app-toast-wrapper position-fixed d-flex justify-content-center"
         style="bottom:28px; left:50%; transform:translateX(-50%); z-index:9999; pointer-events:none;">
        <div class="toast align-items-center border-0 shadow-lg show"
             role="alert" aria-live="assertive" aria-atomic="true"
             style="background:#1d1d1f; color:#f5f5f7; border-radius:12px;
                    padding:12px 20px; min-width:280px; max-width:420px;
                    display:flex; gap:10px; align-items:center; pointer-events:auto;">
            <span style="font-size:1.1rem;">${icon}</span>
            <span style="font-size:0.9rem; flex:1;">${message}</span>
        </div>
    </div>`;

    var el = $(html).appendTo('body');

    // Auto-remove after 2.8s with fade
    setTimeout(function () {
        el.find('.toast').css({ transition: 'opacity 0.35s', opacity: 0 });
        setTimeout(function () { el.remove(); }, 380);
    }, 2800);
}
