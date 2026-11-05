import { Flex, Tag } from "antd";

/**
 * Bộ lọc nhanh dạng chip, luôn có chip "Tất cả" ở đầu.
 *
 * Props:
 * - options:  [{ value, label }]
 * - value:    giá trị đang chọn, undefined nghĩa là "Tất cả"
 * - onChange: (value | undefined) => void
 */
export default function QuickFilter({ options, value, onChange, label = "Lọc nhanh" }) {
    const items = [{ value: "", label: "Tất cả" }, ...options];

    return (
        <Flex wrap gap={8} role="group" aria-label={label}>
            {items.map((item) => (
                <Tag.CheckableTag
                    key={item.value || "all"}
                    checked={(value ?? "") === item.value}
                    onChange={() => onChange(item.value || undefined)}
                    style={{ padding: "4px 14px", fontSize: 14, borderRadius: 999, border: "1px solid #F0E3D8" }}
                >
                    {item.label}
                </Tag.CheckableTag>
            ))}
        </Flex>
    );
}
