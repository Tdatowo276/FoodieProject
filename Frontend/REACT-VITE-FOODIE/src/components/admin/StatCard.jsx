import { Avatar, Card, Flex, Statistic, Typography } from "antd";
import { ArrowDownOutlined, ArrowUpOutlined } from "@ant-design/icons";

import { PRIMARY, display } from "../../utils/helpers";

const { Text } = Typography;

const defaultFormat = (v) => Number(v).toLocaleString("vi-VN");

/**
 * Props:
 * - title, value, growth (% so với kỳ trước, có thể âm)
 * - icon:   icon của Ant Design
 * - format: (value) => string, mặc định định dạng số kiểu vi-VN
 */
export default function StatCard({ title, value, growth, icon, format = defaultFormat, loading }) {
    const isUp = growth >= 0;

    return (
        <Card loading={loading} style={{ height: "100%" }} styles={{ body: { padding: 22 } }}>
            <Flex justify="space-between" align="flex-start" gap={12}>
                <Statistic
                    title={title}
                    value={value}
                    formatter={(v) => <span style={{ ...display, color: PRIMARY, fontSize: 30 }}>{format(v)}</span>}
                />
                <Avatar
                    shape="square"
                    size={48}
                    icon={icon}
                    style={{ background: "#FFF1DC", color: PRIMARY, borderRadius: 14, fontSize: 22, flexShrink: 0 }}
                />
            </Flex>
            <div style={{ marginTop: 10 }}>
                <Text type={isUp ? "success" : "danger"} strong>
                    {isUp ? <ArrowUpOutlined /> : <ArrowDownOutlined />} {Math.abs(growth)}%
                </Text>
                <Text type="secondary"> so với tháng trước</Text>
            </div>
        </Card>
    );
}
