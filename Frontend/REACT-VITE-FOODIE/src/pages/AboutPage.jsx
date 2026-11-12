import React, { useState } from 'react';
import { Typography, Row, Col, Card, Tag, Flex, Avatar, Divider } from 'antd';
import { CheckOutlined, MailOutlined } from '@ant-design/icons';
import Container from '../components/Container';
import { project, team } from '../config/about';
import { PRIMARY, PRIMARY_DARK, display } from '../utils/helpers';

const { Title, Text, Paragraph } = Typography;

const customerFeatures = [
    'Xem thực đơn theo danh mục, tìm món không cần gõ dấu',
    'Giỏ hàng, đặt món và chọn địa chỉ giao hàng mặc định',
    'Theo dõi tiến độ đơn: chờ xác nhận → chuẩn bị → đang giao → hoàn thành',
    'Quản lý tài khoản: ảnh đại diện, họ tên, mật khẩu, địa chỉ',
];

const adminFeatures = [
    'Thống kê doanh thu, số đơn, khách hàng theo tháng',
    'Quản lý món ăn, danh mục, ảnh món',
    'Xử lý đơn hàng theo đúng luồng trạng thái',
    'Quản lý người dùng, khóa / mở khóa tài khoản',
];

const techStack = [
    { group: 'Giao diện khách hàng', items: ['React 18', 'Vite', 'Ant Design'] },
    { group: 'Trang quản trị', items: ['ASP.NET Core MVC', 'Razor', 'Bootstrap 5'] },
    { group: 'Máy chủ & dữ liệu', items: ['ASP.NET Core Web API', 'Entity Framework Core', 'SQL Server'] },
    { group: 'Bảo mật & tài liệu', items: ['JWT', 'Cookie + Anti-forgery', 'BCrypt', 'Swagger / OpenAPI'] },
];

const FeatureList = ({ title, items }) => (
    <Card style={{ height: '100%', borderRadius: 8 }}>
        <Title level={4} style={{ ...display, marginTop: 0 }}>{title}</Title>
        <Flex vertical gap={10}>
            {items.map((f) => (
                <Flex key={f} gap={10} align="start">
                    <CheckOutlined style={{ color: PRIMARY, marginTop: 5 }} />
                    <Text>{f}</Text>
                </Flex>
            ))}
        </Flex>
    </Card>
);

/** Thẻ thành viên. Ảnh lỗi / chưa có => chữ cái đầu của tên. */
const MemberCard = ({ member }) => {
    const [broken, setBroken] = useState(false);
    const initials = member.name.split(' ').filter(Boolean).slice(-1)[0]?.charAt(0).toUpperCase();
    const showPhoto = member.avatar && !broken;

    return (
        <Card style={{ height: '100%', borderRadius: 8, textAlign: 'center' }} styles={{ body: { padding: 28 } }}>
            <Avatar
                size={112}
                src={showPhoto ? member.avatar : undefined}
                onError={() => { setBroken(true); return false; }}
                style={{ background: showPhoto ? '#fff' : PRIMARY, fontSize: 44, marginBottom: 16 }}
            >
                {initials}
            </Avatar>
            <Title level={4} style={{ ...display, margin: 0 }}>{member.name}</Title>
            {member.role && <Text type="secondary" style={{ display: 'block', marginTop: 4 }}>{member.role}</Text>}
            {member.studentId && <Tag style={{ marginTop: 10 }}>MSSV: {member.studentId}</Tag>}
            {member.email && (
                <div style={{ marginTop: 10 }}>
                    <MailOutlined style={{ marginRight: 6, color: PRIMARY }} />
                    <Text copyable>{member.email}</Text>
                </div>
            )}
            {member.tasks?.length > 0 && (
                <Flex wrap gap={6} justify="center" style={{ marginTop: 14 }}>
                    {member.tasks.map((t) => <Tag key={t} color="red" bordered={false}>{t}</Tag>)}
                </Flex>
            )}
        </Card>
    );
};

/** Trang "Về chúng tôi": giới thiệu hệ thống Foodie và nhóm thực hiện. */
const AboutPage = () => {
    const projectInfo = [
        ['Môn học', project.course],
        ['Trường / Khoa', project.school],
        ['Giảng viên hướng dẫn', project.lecturer],
        ['Năm thực hiện', project.year],
    ].filter(([, v]) => v);

    return (
        <>
            <section style={{ background: PRIMARY, color: '#fff', padding: '56px 0 48px' }}>
                <Container>
                    <Title level={1} style={{ ...display, color: '#fff', margin: 0, fontSize: 'clamp(32px, 5vw, 48px)', textTransform: 'uppercase' }}>
                        Về chúng tôi
                    </Title>
                    <Paragraph style={{ color: '#fff', opacity: 0.92, fontSize: 18, maxWidth: 640, margin: '12px 0 0' }}>
                        {project.name}: {project.tagline.toLowerCase()}. Một hệ thống đặt đồ ăn trực tuyến được nhóm
                        xây dựng từ giao diện khách hàng, trang quản trị đến máy chủ và cơ sở dữ liệu.
                    </Paragraph>
                </Container>
            </section>

            <Container style={{ paddingTop: 48 }}>
                {/* Giới thiệu hệ thống */}
                <Title level={2} style={{ ...display, textTransform: 'uppercase', fontSize: 'clamp(22px, 3vw, 30px)' }}>
                    Hệ thống {project.name}
                </Title>
                <Paragraph style={{ fontSize: 16, maxWidth: 820 }}>
                    {project.name} cho phép khách hàng xem thực đơn, đặt món và theo dõi đơn hàng ngay trên trình duyệt,
                    đồng thời cung cấp cho quản trị viên một trang quản lý riêng để cập nhật thực đơn và xử lý đơn.
                    Hệ thống có hai vai trò với quyền hạn khác nhau, và mọi quyền đều được kiểm tra ở phía máy chủ.
                </Paragraph>

                <Row gutter={[20, 20]} style={{ marginTop: 24 }}>
                    <Col xs={24} md={12}><FeatureList title="Dành cho khách hàng" items={customerFeatures} /></Col>
                    <Col xs={24} md={12}><FeatureList title="Dành cho quản trị viên" items={adminFeatures} /></Col>
                </Row>

                <Title level={3} style={{ ...display, marginTop: 40 }}>Công nghệ sử dụng</Title>
                <Row gutter={[20, 16]}>
                    {techStack.map((g) => (
                        <Col key={g.group} xs={24} sm={12} lg={6}>
                            <Text strong style={{ display: 'block', marginBottom: 8 }}>{g.group}</Text>
                            <Flex wrap gap={6}>
                                {g.items.map((t) => <Tag key={t} style={{ margin: 0, padding: '2px 10px' }}>{t}</Tag>)}
                            </Flex>
                        </Col>
                    ))}
                </Row>

                <Divider style={{ margin: '48px 0' }} />

                {/* Nhóm thực hiện */}
                <Title level={2} style={{ ...display, textTransform: 'uppercase', textAlign: 'center', fontSize: 'clamp(22px, 3vw, 30px)' }}>
                    Nhóm thực hiện
                </Title>
                <Row gutter={[24, 24]} justify="center" style={{ marginTop: 24 }}>
                    {team.map((m) => (
                        <Col key={m.name} xs={24} sm={12} md={8}>
                            <MemberCard member={m} />
                        </Col>
                    ))}
                </Row>

                {projectInfo.length > 0 && (
                    <Card style={{ marginTop: 40, borderRadius: 8, background: '#FAFAFA' }}>
                        <Row gutter={[24, 12]}>
                            {projectInfo.map(([label, value]) => (
                                <Col key={label} xs={24} sm={12} md={6}>
                                    <Text type="secondary" style={{ display: 'block', fontSize: 13 }}>{label}</Text>
                                    <Text strong style={{ color: PRIMARY_DARK }}>{value}</Text>
                                </Col>
                            ))}
                        </Row>
                    </Card>
                )}
            </Container>
        </>
    );
};

export default AboutPage;
