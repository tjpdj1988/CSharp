<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="default.aspx.cs" Inherits="Utility._default" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
        <div>
            <div>
                <table style="width:100%;">
                    <tr>
                        <td>用户名：</td>
                        <td>
                            <asp:TextBox ID="TextBox1" runat="server" CssClass="Name@User"></asp:TextBox>
                        </td>
                        <td>&nbsp;</td>
                    </tr>
                    <tr>
                        <td>性别？：</td>
                        <td>
                            <asp:RadioButtonList ID="RadioButtonList1" runat="server" RepeatDirection="Horizontal" CssClass="sex@User">
                                <asp:ListItem Value="1">男</asp:ListItem>
                                <asp:ListItem Value="0">女</asp:ListItem>
                            </asp:RadioButtonList>
                        </td>
                        <td>&nbsp;</td>
                    </tr>
                    <tr>
                        <td>年龄？：</td>
                        <td>
                            <asp:DropDownList ID="DropDownList1" runat="server" CssClass="Age@User">
                                <asp:ListItem>18</asp:ListItem>
                                <asp:ListItem>20</asp:ListItem>
                                <asp:ListItem>25</asp:ListItem>
                            </asp:DropDownList>
                        </td>
                        <td>&nbsp;</td>
                    </tr>
                    <tr>
                        <td>&nbsp;</td>
                        <td>
                            <asp:CheckBoxList ID="CheckBoxList1" runat="server" RepeatDirection="Horizontal">
                                <asp:ListItem>QQ</asp:ListItem>
                                <asp:ListItem>Yohoo</asp:ListItem>
                                <asp:ListItem>Baidu Hi</asp:ListItem>
                            </asp:CheckBoxList>
                        </td>
                        <td>&nbsp;</td>
                    </tr>
                    <tr>
                        <td>&nbsp;</td>
                        <td>
                            <asp:Calendar ID="Calendar1" runat="server" CssClass="Birthday@User"></asp:Calendar>
                        </td>
                        <td>&nbsp;</td>
                    </tr>
                    <tr>
                        <td>&nbsp;</td>
                        <td>
                            <asp:CheckBox ID="CheckBox1" runat="server" CssClass="Remember@User" Text="Remember"/>
                        </td>
                        <td>&nbsp;</td>
                    </tr>
                    <tr>
                        <td>&nbsp;</td>
                        <td>
                            <asp:TextBox ID="TextBox2" runat="server" CssClass="LoginTime@User"></asp:TextBox>
                        </td>
                        <td>&nbsp;</td>
                    </tr>
                    <tr>
                        <td>&nbsp;</td>
                        <td>
                            <asp:Button ID="Button1" runat="server" Text="Button" OnClick="Button1_Click" />
                        </td>
                        <td>&nbsp;</td>
                    </tr>
                    <tr>
                        <td>&nbsp;</td>
                        <td>&nbsp;</td>
                        <td>&nbsp;</td>
                    </tr>
                </table>
            </div>
        </div>
        <asp:Literal ID="Literal1" runat="server"></asp:Literal>
    </form>
</body>
</html>
