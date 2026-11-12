import React, { useState, useEffect, useMemo } from 'react';
import { Typography, Input, Alert, Spin, Empty, Button, Flex } from 'antd';
import { SearchOutlined } from '@ant-design/icons';
import Container from '../components/Container';
import ProductGrid from '../components/products/ProductGrid';
import BannerCarousel from '../components/home/BannerCarousel';
import MenuTabs from '../components/home/MenuTabs';
import { productService, categoryService } from '../services/product.service';
import { PRIMARY, PRIMARY_DARK, display } from '../utils/helpers';

const { Title, Text } = Typography;
const FEATURED_KEY = 'noi-bat';
const BEST_SELLER_KEY = 'ban-chay';

/**
 * Trang chủ: banner → ô tìm món → thanh tab danh mục → mỗi danh mục một khu.
 *  - onSearch(keyword): mở trang Thực đơn với từ khóa đã nhập
 *  - user: ẩn lời mời đăng ký khi đã đăng nhập
 */
const HomePage = ({ setView, onAdd, onOpen, onSearch, user }) => {
    const [featured, setFeatured] = useState([]);
    const [categories, setCategories] = useState([]);
    const [products, setProducts] = useState([]);
    const [bestSellerIds, setBestSellerIds] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState('');
    const [active, setActive] = useState(FEATURED_KEY);
    const [keyword, setKeyword] = useState('');

    useEffect(() => {
        let ignore = false;
        Promise.all([
            productService.getFeatured(),
            categoryService.getAll(),
            productService.getAll(),
            // Món bán chạy không bắt buộc: lỗi thì vẫn hiện trang chủ bình thường
            productService.getBestSellers(8).catch(() => []),
        ])
            .then(([f, c, p, best]) => {
                if (ignore) return;
                setFeatured(f);
                setCategories(c);
                setProducts(p);
                setBestSellerIds(best.map((b) => b.productId));
            })
            .catch((e) => !ignore && setError(e.message))
            .finally(() => !ignore && setLoading(false));
        return () => { ignore = true; };
    }, []);

    // Mỗi danh mục một khu; bỏ danh mục chưa có món
    const sections = useMemo(() => {
        const list = [];
        if (featured.length > 0) list.push({ key: FEATURED_KEY, label: 'Nổi bật', items: featured });
        // Bán chạy 30 ngày qua (theo đơn đã hoàn thành), giữ đúng thứ tự xếp hạng
        const bestSellers = bestSellerIds.map((id) => products.find((p) => p.id === id)).filter(Boolean);
        if (bestSellers.length > 0) list.push({ key: BEST_SELLER_KEY, label: 'Bán chạy', items: bestSellers });
        for (const c of categories) {
            const items = products.filter((p) => p.categoryId === c.id);
            if (items.length > 0) list.push({ key: `danh-muc-${c.id}`, label: c.name, items });
        }
        return list;
    }, [featured, categories, products, bestSellerIds]);

    const tabs = useMemo(() => sections.map(({ key, label }) => ({ key, label })), [sections]);

    const search = () => {
        const kw = keyword.trim();
        if (kw) onSearch(kw);
    };

    return (
        <>
            <BannerCarousel />

            {/* Dải tìm kiếm */}
            <div style={{ background: PRIMARY, padding: '18px 16px' }}>
                <Input
                    size="large"
                    allowClear
                    value={keyword}
                    onChange={(e) => setKeyword(e.target.value)}
                    onPressEnter={search}
                    placeholder="Tìm món..."
                    aria-label="Tìm món"
                    suffix={<SearchOutlined onClick={search} style={{ color: PRIMARY, fontSize: 20, cursor: 'pointer' }} aria-label="Tìm" />}
                    style={{ display: 'flex', maxWidth: 660, margin: '0 auto', borderRadius: 999, paddingInline: 20 }}
                />
            </div>

            <Title level={2} style={{ ...display, textAlign: 'center', margin: '48px 16px 28px', fontSize: 'clamp(26px, 4vw, 40px)' }}>
                Thực đơn ngon - Đặt món ngay
            </Title>

            {error ? (
                <Container><Alert type="error" showIcon message="Không tải được thực đơn" description={error} /></Container>
            ) : loading ? (
                <Flex justify="center" style={{ padding: 60 }}><Spin size="large" /></Flex>
            ) : sections.length === 0 ? (
                <Empty description="Thực đơn đang được cập nhật." style={{ padding: 48 }} />
            ) : (
                <>
                    <MenuTabs tabs={tabs} active={active} onChange={setActive} />
                    <Container>
                        {sections.map((s) => (
                            <section key={s.key} id={s.key} aria-labelledby={`${s.key}-title`} style={{ paddingTop: 40 }}>
                                <Title
                                    id={`${s.key}-title`}
                                    level={3}
                                    style={{ ...display, textTransform: 'uppercase', margin: '0 0 20px', fontSize: 'clamp(22px, 3vw, 30px)' }}
                                >
                                    {s.label}
                                </Title>
                                <ProductGrid products={s.items} onAdd={onAdd} onOpen={onOpen} />
                            </section>
                        ))}
                    </Container>
                </>
            )}

            {/* Lời mời đăng ký: chỉ dành cho khách chưa có tài khoản */}
            {!user && (
                <Container style={{ marginTop: 56 }}>
                    <Flex
                        justify="space-between"
                        align="center"
                        wrap
                        gap={20}
                        style={{ background: PRIMARY_DARK, color: '#fff', borderRadius: 8, padding: '28px 32px' }}
                    >
                        <div>
                            <Title level={3} style={{ ...display, color: '#fff', margin: 0 }}>Thành viên mới giảm 20%</Title>
                            <Text style={{ color: '#fff', opacity: 0.85 }}>Tạo tài khoản để nhận ưu đãi cho đơn đầu tiên và theo dõi đơn hàng.</Text>
                        </div>
                        <Button size="large" onClick={() => setView('register')} style={{ fontWeight: 600, color: PRIMARY_DARK }}>
                            Đăng ký ngay
                        </Button>
                    </Flex>
                </Container>
            )}
        </>
    );
};

export default HomePage;
