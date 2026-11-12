import React, { useCallback, useEffect, useState } from 'react';
import {
    Card, Row, Col, Typography, Rate, Progress, List, Avatar, Empty, Pagination, Flex, Form, Input, Button,
    Alert, Popconfirm, App as AntApp, Spin,
} from 'antd';
import { UserOutlined } from '@ant-design/icons';
import { reviewService } from '../../services/review.service';
import { PRIMARY, display, formatDateTime, resolveImageUrl } from '../../utils/helpers';

const { Title, Text, Paragraph } = Typography;
const PAGE_SIZE = 5;
const STAR_LABELS = ['Rất tệ', 'Chưa ngon', 'Bình thường', 'Ngon', 'Tuyệt vời'];

/** Điểm trung bình + số lượt theo từng mức sao. */
const Summary = ({ summary }) => (
    <Row gutter={[24, 16]} align="middle">
        <Col xs={24} sm={8} style={{ textAlign: 'center' }}>
            <div style={{ ...display, fontSize: 48, lineHeight: 1, color: PRIMARY }}>
                {summary.count ? summary.average.toFixed(1) : '–'}
            </div>
            <Rate disabled allowHalf value={summary.average} style={{ fontSize: 16 }} />
            <div><Text type="secondary">{summary.count} lượt đánh giá</Text></div>
        </Col>
        <Col xs={24} sm={16}>
            {[5, 4, 3, 2, 1].map((star) => {
                const n = summary.stars[star - 1];
                return (
                    <Flex key={star} align="center" gap={8}>
                        <Text style={{ width: 42 }}>{star} sao</Text>
                        <Progress
                            percent={summary.count ? Math.round((n / summary.count) * 100) : 0}
                            showInfo={false}
                            strokeColor="#FADB14"
                            style={{ flex: 1, margin: 0 }}
                            aria-label={`${star} sao: ${n} lượt`}
                        />
                        <Text type="secondary" style={{ width: 28, textAlign: 'right' }}>{n}</Text>
                    </Flex>
                );
            })}
        </Col>
    </Row>
);

/** Ô đánh giá của người đang đăng nhập (thêm / sửa / xóa). */
const MyReview = ({ productId, user, onChanged, onNavigate }) => {
    const { message } = AntApp.useApp();
    const [form] = Form.useForm();
    const [state, setState] = useState(null);       // { canReview, reason, review }
    const [saving, setSaving] = useState(false);
    const [editing, setEditing] = useState(false);

    const load = useCallback(() => {
        if (!user) return;
        reviewService.getMine(productId).then((s) => {
            setState(s);
            setEditing(!s.review);
            form.setFieldsValue({ rating: s.review?.rating ?? 0, comment: s.review?.comment ?? '' });
        }).catch(() => setState(null));
    }, [productId, user, form]);

    useEffect(() => { load(); }, [load]);

    if (!user) {
        return (
            <Alert
                type="info"
                showIcon
                message="Đăng nhập để đánh giá món đã mua."
                action={<Button size="small" onClick={() => onNavigate('login')}>Đăng nhập</Button>}
            />
        );
    }
    if (!state) return null;
    if (!state.canReview && !state.review) {
        return <Alert type="info" showIcon message={state.reason ?? 'Bạn chưa thể đánh giá món này.'} />;
    }

    const save = async ({ rating, comment }) => {
        setSaving(true);
        try {
            await reviewService.save(productId, { rating, comment: comment?.trim() });
            message.success(state.review ? 'Đã cập nhật đánh giá.' : 'Cảm ơn bạn đã đánh giá!');
            load();
            onChanged();
        } catch (e) {
            message.error(e.message);
        } finally {
            setSaving(false);
        }
    };

    const remove = async () => {
        try {
            await reviewService.remove(productId);
            message.success('Đã xóa đánh giá.');
            load();
            onChanged();
        } catch (e) {
            message.error(e.message);
        }
    };

    if (state.review && !editing) {
        return (
            <Card size="small" style={{ background: '#FFF8F8', borderColor: '#FFD6D6' }}>
                <Flex justify="space-between" align="start" wrap gap={8}>
                    <div>
                        <Text strong>Đánh giá của bạn</Text>
                        <div><Rate disabled value={state.review.rating} style={{ fontSize: 14 }} /></div>
                        {state.review.comment && <Paragraph style={{ margin: '4px 0 0', whiteSpace: 'pre-line' }}>{state.review.comment}</Paragraph>}
                    </div>
                    <Flex gap={8}>
                        <Button size="small" onClick={() => setEditing(true)}>Sửa</Button>
                        <Popconfirm title="Xóa đánh giá của bạn?" okText="Xóa" cancelText="Hủy" onConfirm={remove}>
                            <Button size="small" danger>Xóa</Button>
                        </Popconfirm>
                    </Flex>
                </Flex>
            </Card>
        );
    }

    return (
        <Card size="small" title={state.review ? 'Sửa đánh giá của bạn' : 'Viết đánh giá'}>
            <Form form={form} layout="vertical" onFinish={save} requiredMark={false}>
                <Form.Item
                    name="rating"
                    label="Bạn chấm mấy sao?"
                    rules={[{ validator: (_, v) => (v >= 1 ? Promise.resolve() : Promise.reject(new Error('Vui lòng chọn số sao.'))) }]}
                >
                    <Rate tooltips={STAR_LABELS} />
                </Form.Item>
                <Form.Item name="comment" label="Nhận xét (không bắt buộc)" rules={[{ max: 500 }]}>
                    <Input.TextArea rows={3} showCount maxLength={500} placeholder="Món ăn thế nào? Hương vị, khẩu phần..." />
                </Form.Item>
                <Flex gap={8}>
                    <Button type="primary" htmlType="submit" loading={saving}>Gửi đánh giá</Button>
                    {state.review && <Button onClick={() => setEditing(false)}>Hủy</Button>}
                </Flex>
            </Form>
        </Card>
    );
};

/**
 * Phần đánh giá ở trang chi tiết món.
 *  - onRatingChanged(): gọi khi đánh giá thay đổi (để trang tải lại điểm trung bình của món)
 */
const ReviewsSection = ({ productId, user, onNavigate, onRatingChanged }) => {
    const [page, setPage] = useState(1);
    const [data, setData] = useState(null);
    const [error, setError] = useState('');

    const load = useCallback(async () => {
        try {
            setData(await reviewService.getForProduct(productId, { page, pageSize: PAGE_SIZE }));
            setError('');
        } catch (e) {
            setError(e.message);
        }
    }, [productId, page]);

    useEffect(() => { load(); }, [load]);

    const handleChanged = () => {
        setPage(1);
        load();
        onRatingChanged?.();
    };

    return (
        <Card style={{ marginTop: 24, borderRadius: 8 }} styles={{ body: { padding: 28 } }}>
            <Title level={3} style={{ ...display, marginTop: 0 }}>Đánh giá món ăn</Title>

            {error && <Alert type="error" showIcon message="Không tải được đánh giá" description={error} />}
            {!error && !data && <Flex justify="center" style={{ padding: 32 }}><Spin /></Flex>}

            {data && (
                <Flex vertical gap={24}>
                    <Summary summary={data.summary} />
                    <MyReview productId={productId} user={user} onChanged={handleChanged} onNavigate={onNavigate} />

                    {data.reviews.total === 0 ? (
                        <Empty description="Chưa có đánh giá nào. Hãy là người đầu tiên sau khi thưởng thức món này!" />
                    ) : (
                        <>
                            <List
                                itemLayout="horizontal"
                                dataSource={data.reviews.items}
                                renderItem={(r) => (
                                    <List.Item>
                                        <List.Item.Meta
                                            avatar={<Avatar src={resolveImageUrl(r.userAvatarUrl)} icon={<UserOutlined />} />}
                                            title={
                                                <Flex gap={8} wrap align="center">
                                                    <Text strong>{r.userName}</Text>
                                                    <Rate disabled value={r.rating} style={{ fontSize: 12 }} />
                                                    <Text type="secondary" style={{ fontSize: 12 }}>
                                                        {formatDateTime(r.updatedAt ?? r.createdAt)}{r.updatedAt ? ' (đã sửa)' : ''}
                                                    </Text>
                                                </Flex>
                                            }
                                            // React tự mã hóa nội dung => an toàn với chuỗi chứa thẻ HTML (chống XSS)
                                            description={r.comment && <span style={{ color: '#333', whiteSpace: 'pre-line' }}>{r.comment}</span>}
                                        />
                                    </List.Item>
                                )}
                            />
                            {data.reviews.total > PAGE_SIZE && (
                                <Flex justify="center">
                                    <Pagination current={page} pageSize={PAGE_SIZE} total={data.reviews.total} onChange={setPage} showSizeChanger={false} />
                                </Flex>
                            )}
                        </>
                    )}
                </Flex>
            )}
        </Card>
    );
};

export default ReviewsSection;
