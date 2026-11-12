import React, { useState, useEffect, useCallback } from 'react';
import {
    Typography, List, Tag, Alert, Button, Empty, Modal, Spin, Steps, Descriptions, Table, Flex, Pagination,
} from 'antd';
import { ShoppingOutlined, ReloadOutlined, StarOutlined } from '@ant-design/icons';
import Container from '../components/Container';
import { orderService } from '../services/auth.service';
import { ORDER_STATUS, display, formatVND, formatDateTime } from '../utils/helpers';

const { Title, Text } = Typography;
const PAGE_SIZE = 10;
const FLOW = ['pending', 'preparing', 'delivering', 'completed'];

const StatusTag = ({ status }) => {
    const s = ORDER_STATUS[status] ?? { label: status, color: 'default' };
    return <Tag color={s.color} style={{ marginInlineEnd: 0 }}>{s.label}</Tag>;
};

/** Chi tiết một đơn: tiến độ, món, tiền, thông tin giao hàng. */
const OrderDetailModal = ({ code, onClose, onOpenProduct }) => {
    const [order, setOrder] = useState(null);
    const [error, setError] = useState('');

    useEffect(() => {
        if (!code) return;
        let ignore = false;
        setOrder(null);
        setError('');
        orderService
            .getByCode(code)
            .then((data) => { if (!ignore) setOrder(data); })
            .catch((e) => { if (!ignore) setError(e.message); });
        return () => { ignore = true; };
    }, [code]);

    const cancelled = order?.status === 'cancelled';
    const current = Math.max(0, FLOW.indexOf(order?.status));

    return (
        <Modal open={!!code} onCancel={onClose} footer={null} title={`Đơn ${code ?? ''}`} width={640} centered destroyOnClose>
            {error && <Alert type="error" showIcon message={error} />}
            {!error && !order && <Flex justify="center" style={{ padding: 40 }}><Spin /></Flex>}
            {order && (
                <Flex vertical gap={20}>
                    {cancelled ? (
                        <Alert type="warning" showIcon message="Đơn hàng đã bị hủy." />
                    ) : (
                        <Steps
                            size="small"
                            responsive
                            current={current}
                            status={order.status === 'completed' ? 'finish' : 'process'}
                            items={FLOW.map((s) => ({ title: ORDER_STATUS[s].label }))}
                        />
                    )}

                    <Table
                        size="small"
                        rowKey={(i, idx) => `${i.productName}-${idx}`}
                        pagination={false}
                        dataSource={order.items}
                        scroll={{ x: 'max-content' }}
                        columns={[
                            { title: 'Món', dataIndex: 'productName' },
                            { title: 'SL', dataIndex: 'quantity', align: 'center', width: 56 },
                            { title: 'Thành tiền', dataIndex: 'lineTotal', align: 'right', render: formatVND },
                            // Đơn đã hoàn thành: mời đánh giá từng món (món đã ngừng bán thì không có nút)
                            ...(order.status === 'completed' && onOpenProduct
                                ? [{
                                    title: '',
                                    key: 'review',
                                    align: 'right',
                                    width: 96,
                                    render: (_, item) => item.productId && (
                                        <Button size="small" icon={<StarOutlined />} onClick={() => { onClose(); onOpenProduct(item.productId); }}>
                                            Đánh giá
                                        </Button>
                                    ),
                                }]
                                : []),
                        ]}
                        summary={() => (
                            <>
                                <Table.Summary.Row>
                                    <Table.Summary.Cell index={0} colSpan={2}>Phí giao hàng</Table.Summary.Cell>
                                    <Table.Summary.Cell index={1} align="right">
                                        {order.shippingFee === 0 ? 'Miễn phí' : formatVND(order.shippingFee)}
                                    </Table.Summary.Cell>
                                </Table.Summary.Row>
                                <Table.Summary.Row>
                                    <Table.Summary.Cell index={0} colSpan={2}><Text strong>Tổng cộng</Text></Table.Summary.Cell>
                                    <Table.Summary.Cell index={1} align="right"><Text strong>{formatVND(order.total)}</Text></Table.Summary.Cell>
                                </Table.Summary.Row>
                            </>
                        )}
                    />

                    <Descriptions size="small" column={1} bordered>
                        <Descriptions.Item label="Trạng thái"><StatusTag status={order.status} /></Descriptions.Item>
                        <Descriptions.Item label="Đặt lúc">{formatDateTime(order.createdAt)}</Descriptions.Item>
                        <Descriptions.Item label="Người nhận">{order.customerName} · {order.phone}</Descriptions.Item>
                        <Descriptions.Item label="Địa chỉ">{order.address}</Descriptions.Item>
                        {order.note && <Descriptions.Item label="Ghi chú">{order.note}</Descriptions.Item>}
                        <Descriptions.Item label="Thanh toán">
                            {order.paymentMethod === 'bank' ? 'Chuyển khoản' : 'Tiền mặt khi nhận hàng'}
                        </Descriptions.Item>
                    </Descriptions>
                </Flex>
            )}
        </Modal>
    );
};

/** Trang "Đơn của tôi": danh sách đơn của tài khoản đang đăng nhập (GET /api/orders/me). */
const MyOrdersPage = ({ onNavigate, openCode, onOpenProduct }) => {
    const [page, setPage] = useState(1);
    const [data, setData] = useState({ items: [], total: 0 });
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState('');
    const [selected, setSelected] = useState(openCode ?? null);

    const load = useCallback(async (p) => {
        setLoading(true);
        try {
            setData(await orderService.getMine({ page: p, pageSize: PAGE_SIZE }));
            setError('');
        } catch (e) {
            setError(e.message);
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => { load(page); }, [page, load]);

    return (
        <Container style={{ paddingBottom: 24, maxWidth: 820 }}>
            <Flex justify="space-between" align="center" wrap gap={12} style={{ padding: '36px 0 20px' }}>
                <div>
                    <Title level={1} style={{ ...display, margin: 0 }}>Đơn của tôi</Title>
                    <Text type="secondary">Theo dõi tình trạng các đơn bạn đã đặt.</Text>
                </div>
                <Button icon={<ReloadOutlined />} onClick={() => load(page)} loading={loading}>Làm mới</Button>
            </Flex>

            {error ? (
                <Alert type="error" showIcon message="Không tải được đơn hàng" description={error} />
            ) : (
                <Spin spinning={loading}>
                    {!loading && data.total === 0 ? (
                        <Empty description="Bạn chưa có đơn hàng nào." style={{ padding: '48px 0' }}>
                            <Button type="primary" icon={<ShoppingOutlined />} onClick={() => onNavigate('products')}>
                                Xem thực đơn
                            </Button>
                        </Empty>
                    ) : (
                        <List
                            bordered
                            style={{ background: '#fff' }}
                            dataSource={data.items}
                            renderItem={(o) => (
                                <List.Item
                                    style={{ cursor: 'pointer' }}
                                    onClick={() => setSelected(o.code)}
                                    actions={[<Button key="view" type="link" onClick={() => setSelected(o.code)}>Chi tiết</Button>]}
                                >
                                    <List.Item.Meta
                                        title={<Flex gap={8} wrap align="center"><Text strong>{o.code}</Text><StatusTag status={o.status} /></Flex>}
                                        description={`${formatDateTime(o.createdAt)} · ${o.itemCount} phần`}
                                    />
                                    <Text strong style={{ whiteSpace: 'nowrap' }}>{formatVND(o.total)}</Text>
                                </List.Item>
                            )}
                        />
                    )}
                </Spin>
            )}

            {data.total > PAGE_SIZE && (
                <Flex justify="center" style={{ marginTop: 16 }}>
                    <Pagination current={page} pageSize={PAGE_SIZE} total={data.total} onChange={setPage} showSizeChanger={false} />
                </Flex>
            )}

            <OrderDetailModal code={selected} onClose={() => setSelected(null)} onOpenProduct={onOpenProduct} />
        </Container>
    );
};

export default MyOrdersPage;
