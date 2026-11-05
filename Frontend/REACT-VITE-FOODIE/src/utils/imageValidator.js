/**
 * Kiểm tra file ảnh phía client TRƯỚC khi upload.
 * Đây chỉ là lớp bảo vệ trải nghiệm người dùng, backend vẫn phải kiểm tra lại.
 * Có thể gộp file này vào src/utils/validators.js nếu bạn muốn.
 */

export const IMAGE_RULES = {
    extensions: ["jpg", "jpeg", "png"],
    maxSizeBytes: 2 * 1024 * 1024, // 2MB
};

const SIGNATURE_LENGTH = 8;

const readHeader = (file) =>
    new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => resolve(new Uint8Array(reader.result));
        reader.onerror = () => reject(new Error("Không đọc được file."));
        reader.readAsArrayBuffer(file.slice(0, SIGNATURE_LENGTH));
    });

const PNG_SIGNATURE = [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a];

const detectKind = (bytes) => {
    if (bytes[0] === 0xff && bytes[1] === 0xd8 && bytes[2] === 0xff) return "jpeg";
    if (PNG_SIGNATURE.every((value, i) => bytes[i] === value)) return "png";
    return null;
};

/**
 * @returns {Promise<{ ok: boolean, message?: string }>}
 */
export async function validateImageFile(file, rules = IMAGE_RULES) {
    const maxMB = rules.maxSizeBytes / 1024 / 1024;
    const allowed = rules.extensions.map((e) => `.${e}`).join(", ");

    // 1. Đuôi file
    const ext = file.name.includes(".") ? file.name.split(".").pop().toLowerCase() : "";
    if (!rules.extensions.includes(ext)) {
        return { ok: false, message: `Chỉ chấp nhận ảnh định dạng ${allowed}.` };
    }

    // 2. Dung lượng
    if (file.size === 0) {
        return { ok: false, message: "File rỗng, vui lòng chọn ảnh khác." };
    }
    if (file.size > rules.maxSizeBytes) {
        const sizeMB = (file.size / 1024 / 1024).toFixed(1);
        return { ok: false, message: `Ảnh nặng ${sizeMB}MB, vượt giới hạn ${maxMB}MB.` };
    }

    // 3. Nội dung thật của file (chữ ký đầu file), tránh đổi đuôi file khác thành .jpg
    let kind;
    try {
        kind = detectKind(await readHeader(file));
    } catch (e) {
        return { ok: false, message: e.message };
    }
    if (!kind) {
        return { ok: false, message: "Nội dung file không phải ảnh JPG hoặc PNG hợp lệ." };
    }

    // 4. Đuôi file và MIME phải khớp với nội dung thật
    const extMatchesKind = kind === "png" ? ext === "png" : ext === "jpg" || ext === "jpeg";
    const expectedMime = kind === "png" ? "image/png" : "image/jpeg";
    if (!extMatchesKind || file.type !== expectedMime) {
        return { ok: false, message: "Đuôi file không khớp với nội dung ảnh." };
    }

    return { ok: true };
}
