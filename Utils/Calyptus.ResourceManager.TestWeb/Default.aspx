<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="TestWeb._Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head>
    <title>Untitled Page</title>
    <%--<c:Import Assembly="Calyptus.ClientSide" Name="GoogleAPIs.MooTools" runat="server" />--%>
    <c:Build Name="CSS/MyStyleB.css" runat="server" />
    <%--<c:Import Src="http://www.fordonslagret.se/js/common.js" runat="server" />--%>
    <%--<c:Build Compress="Never" Name="MooTools.*" runat="server" />--%>
    <c:Import Src="Test.js" runat="server" />
    <c:Import Assembly="Calyptus.ClientSide" Name="MooTools.DomReady.js" runat="server" />
</head>
<body>
<h1>Test header (<%= System.Globalization.CultureInfo.CurrentUICulture.Name %>)</h1>
<c:Import Src="Img/toad.jpg" runat="server" />
<p>Paragraph</p>
<c:Include Src="Img/toad.jpg" runat="server" />
</body>
</html>
