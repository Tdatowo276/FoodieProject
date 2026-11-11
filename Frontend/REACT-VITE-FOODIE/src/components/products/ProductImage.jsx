import { useState } from "react";
import { ShopOutlined } from "@ant-design/icons";
import { resolveImageUrl } from "../../utils/helpers";

/**
 * Ảnh món. Món chưa có ảnh (admin chưa upload) => khung xám nhạt + biểu tượng đơn giản.
 * fit="contain" hợp với ảnh món đã tách nền (nền trắng); "cover" lấp đầy khung.
 */
const ProductImage = ({ product, height = 170, radius = 0, fit = "contain" }) => {
    const [broken, setBroken] = useState(false);
    const src = resolveImageUrl(product.imageUrl);

    if (src && !broken) {
        return (
            <img
                src={src}
                alt={product.name}
                loading="lazy"
                onError={() => setBroken(true)}
                style={{ width: "100%", height, objectFit: fit, display: "block", borderRadius: radius, background: "#fff" }}
            />
        );
    }

    return (
        <div
            style={{
                height,
                width: "100%",
                borderRadius: radius,
                background: "#F5F5F5",
                display: "grid",
                placeItems: "center",
                color: "#BDBDBD",
                fontSize: Math.min(48, height * 0.28),
            }}
            aria-hidden="true"
        >
            <ShopOutlined />
        </div>
    );
};

export default ProductImage;
