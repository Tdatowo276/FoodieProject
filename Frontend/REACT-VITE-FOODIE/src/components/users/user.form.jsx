import { Button, notification } from "antd";
import Input from "antd/es/input/Input";
import { useState } from "react";
import { createUserAPI } from "../../services/api.service";

const UserForm = () => {
    const [fullName, setFullName] = useState("");
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [phone, setPhone] = useState("");
    const handleClick = async () => {
        const res = await createUserAPI(fullName, email, password, phone);
        if (res.data) {
            notification.success({
                message: "Create User",
                description: "Create successfull"
            })
        } else {
            notification.error({
                message: "Create Failed",
                description: JSON.stringify(res.message)
            })
        }
        console.log(res.data);
    }
    return (
        <>
            <div className="user-form" style={{ margin: "20px 0px" }}>
                <div style={{ display: "flex", gap: "15px", flexDirection: "column" }}>
                    <span>FullName: </span>
                    <Input value={fullName}
                        onChange={(event) => { setFullName(event.target.value) }} />
                    <span>Email: </span>
                    <Input value={email}
                        onChange={(event) => { setEmail(event.target.value) }} />
                    <span>Password: </span>
                    <Input.Password value={password}
                        onChange={(event) => { setPassword(event.target.value) }} />
                    <span>PhoneNumber: </span>
                    <Input value={phone}
                        onChange={(event) => { setPhone(event.target.value) }} />
                    <div>
                        <Button type="primary"
                            onClick={handleClick}>Create User</Button>
                    </div>
                </div>
            </div>
        </>
    )
}
export default UserForm;