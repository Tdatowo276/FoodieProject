
const Container = ({ children, style }) => (
    <div style={{ maxWidth: 1180, margin: "0 auto", padding: "0 20px", ...style }}>{children}</div>
);
export default Container;