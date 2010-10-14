<%@ Page Language="C#" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head>
    <title></title>
    <%--<c:Import Assembly="Calyptus.ClientSide" Name="GoogleAPIs.MooTools" runat="server" />--%>
    <c:Build Src="Styles/MyStyleB.css" runat="server" />
    <%--<c:Import Src="http://www.fordonslagret.se/js/common.js" runat="server" />--%>
    <%--<c:Build Compress="Never" Name="MooTools.*" runat="server" />--%>
    <%--<c:Import Src="TestFrameworks.js" runat="server" />--%>
    <%--<c:Import Assembly="Calyptus.ClientSide" Name="MooTools.DomReady.js" runat="server" />--%>
    <c:Import Src="~/Styles/ClusterStyle.css" runat="server" />
    <c:Import Src="~/Styles/MainStyle.less" runat="server" />
</head>
<body>
    <c:Import Src="~/Scripts/ClusterScript.js" runat="server" />
    <c:Import Src="~/Scripts/MainScript.js" runat="server" />
	<c:Import Src="Images/toad.jpg" runat="server" />
	<c:Include Src="Images/toad.jpg" runat="server" />
</body>
</html>