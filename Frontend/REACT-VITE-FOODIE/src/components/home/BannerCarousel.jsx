import React, { useState } from 'react';
import { Carousel } from 'antd';
import banners from '../../config/banners';
import { PRIMARY, PRIMARY_DARK, display } from '../../utils/helpers';

// Tỉ lệ 3:1 (1920x640); trên điện thoại giữ chiều cao tối thiểu để chữ không quá nhỏ
const frame = { width: '100%', aspectRatio: '3 / 1', minHeight: 200, maxHeight: 640, display: 'block' };

/** Một slide: ảnh thật nếu có, nếu chưa có file ảnh thì hiện nền đỏ + tiêu đề. */
const Slide = ({ banner }) => {
    const [missing, setMissing] = useState(false);

    if (!missing) {
        return (
            <img
                src={banner.src}
                alt={banner.alt}
                onError={() => setMissing(true)}
                style={{ ...frame, objectFit: 'cover' }}
            />
        );
    }

    return (
        <div
            role="img"
            aria-label={banner.alt}
            style={{
                ...frame,
                background: `${PRIMARY} radial-gradient(circle at 85% 50%, ${PRIMARY_DARK} 0 22%, transparent 22.5%)`,
                color: '#fff',
                display: 'flex',
                flexDirection: 'column',
                justifyContent: 'center',
                padding: '0 8%',
                boxSizing: 'border-box',
            }}
        >
            <div style={{ ...display, fontSize: 'clamp(28px, 5vw, 64px)', lineHeight: 1.1, textTransform: 'uppercase' }}>
                {banner.title}
            </div>
            <div style={{ fontSize: 'clamp(14px, 1.6vw, 22px)', marginTop: 10, maxWidth: 560, opacity: 0.95 }}>
                {banner.subtitle}
            </div>
        </div>
    );
};

const BannerCarousel = () => (
    <section aria-label="Khuyến mãi" style={{ background: PRIMARY_DARK }}>
        <Carousel autoplay autoplaySpeed={5000} arrows dots draggable>
            {banners.map((b) => (
                <div key={b.src}>
                    <Slide banner={b} />
                </div>
            ))}
        </Carousel>
    </section>
);

export default BannerCarousel;
