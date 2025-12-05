// --- ĐOẠN CODE XỬ LÝ CLICK LINK (LOGIN/REGISTER) ---
const links = document.querySelectorAll('.js-loading');

links.forEach(link => {
    link.addEventListener('click', async function (e) {
        // 1. Chặn chuyển trang ngay lập tức
        e.preventDefault();

        // 2. Lấy đường dẫn đích (href)
        const targetUrl = this.getAttribute('href');

        // 3. Hiện màn hình Loading Full Screen
        // (Đảm bảo bạn đã có <div id="page-loader"> ở cuối body như bài trước)
        const loader = document.getElementById('page-loader');
        if (loader) loader.classList.remove('hidden');

        // 4. Delay 0.5 giây (500ms)
        await new Promise(resolve => setTimeout(resolve, 500));

        // 5. Chuyển trang thủ công
        window.location.href = targetUrl;
    });
});