import { resolveImageUrl } from "../../utils/helpers";
// Hiện ảnh thật nếu API có imageUrl, nếu không thì dùng emoji
const ProductImage = ({ product, height = 170, radius = 0, fontSize }) => {
    if (resolveImageUrl(product.imageUrl)) {
        return (
            <img
                src={resolveImageUrl(product.imageUrl)}
                alt={product.name}
                style={{ width: "100%", height, objectFit: "cover", display: "block", borderRadius: radius }}
            />
        );
    }
    return (
        <div
            style={{
                height,
                background: product.bg || "#FFF1DC",
                display: "grid",
                placeItems: "center",
                fontSize: fontSize || height * 0.5,
                borderRadius: radius,
                width: "100%",
            }}
            aria-hidden="true"
        >
            {product.emoji || "🍽️"}
        </div>
    );
}

export default ProductImage;