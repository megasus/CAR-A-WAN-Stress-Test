using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml;

namespace Carawan_CTRL
{
    internal class Program
    {
        private static TcpClient _globTcp;
        private static string _fullXml;

        private static void Main()
        {
            try
            {
                _globTcp = new TcpClient("192.168.2.1", 46000);
                Console.WriteLine("CARAWAN connection established");
                var listenThread = new Thread(Listen);
                listenThread.Start();

                // Create and load the XML document.
                var doc = new XmlDocument();
                var el = (XmlElement)doc.AppendChild(doc.CreateElement("cawpacket"));
                el.SetAttribute("version", "1.0");
                el.SetAttribute("type", "option-request");
                var optionElement = doc.CreateElement("option");
                optionElement.SetAttribute("option-id", "1");
                optionElement.SetAttribute("type", "set");
                optionElement.SetAttribute("valuename", "autodial");
                optionElement.SetAttribute("value", "no");
                el.AppendChild(optionElement);

                // Create an XML declaration. 
                var xmldecl = doc.CreateXmlDeclaration("1.0", null, null);
                xmldecl.Encoding = "ISO-8859-1";

                // Add the new node to the document.
                var root = doc.DocumentElement;
                doc.InsertBefore(xmldecl, root);

                // Display the modified XML document 
                Console.WriteLine(doc.OuterXml);
                
                // Disable autodialing
                var st = _globTcp.GetStream();
                var iso = Encoding.GetEncoding("ISO-8859-1");
                var bytes = iso.GetBytes(doc.OuterXml);
                st.Write(bytes, 0, bytes.Length);

                // Start sending test commands
                var commandThread = new Thread(TestCommandSender);
                commandThread.Start();
            }
            catch (Exception)
            {
                Console.WriteLine("CARAWAN connection failed");
                Main();
            }
        }

        private static void TestCommandSender()
        {
            for (var i = 0; i >= 0; i++)
            {
                Thread.Sleep(10000);
                if (i%2 == 0)
                {
                    // Create and load the XML document.
                    var doc = new XmlDocument();
                    var el = (XmlElement)doc.AppendChild(doc.CreateElement("cawpacket"));
                    el.SetAttribute("version", "1.0");
                    el.SetAttribute("type", "action-request");
                    var optionElement = doc.CreateElement("action");
                    optionElement.SetAttribute("type", "hangup");
                    optionElement.SetAttribute("module-id", "0");
                    el.AppendChild(optionElement);

                    // Create an XML declaration. 
                    var xmldecl = doc.CreateXmlDeclaration("1.0", null, null);
                    xmldecl.Encoding = "ISO-8859-1";

                    // Add the new node to the document.
                    var root = doc.DocumentElement;
                    doc.InsertBefore(xmldecl, root);

                    // Display the modified XML document 
                    Console.WriteLine(doc.OuterXml);

                    // Hangup module 0
                    var st = _globTcp.GetStream();
                    var iso = Encoding.GetEncoding("ISO-8859-1");
                    var bytes = iso.GetBytes(doc.OuterXml);
                    st.Write(bytes, 0, bytes.Length);
                }
                else
                {
                    // Create and load the XML document.
                    var doc = new XmlDocument();
                    var el = (XmlElement)doc.AppendChild(doc.CreateElement("cawpacket"));
                    el.SetAttribute("version", "1.0");
                    el.SetAttribute("type", "action-request");
                    var optionElement = doc.CreateElement("action");
                    optionElement.SetAttribute("type", "dialin");
                    optionElement.SetAttribute("module-id", "0");
                    el.AppendChild(optionElement);

                    // Create an XML declaration. 
                    var xmldecl = doc.CreateXmlDeclaration("1.0", null, null);
                    xmldecl.Encoding = "ISO-8859-1";

                    // Add the new node to the document.
                    var root = doc.DocumentElement;
                    doc.InsertBefore(xmldecl, root);

                    // Display the modified XML document 
                    Console.WriteLine(doc.OuterXml);

                    // Dial module 0
                    var st = _globTcp.GetStream();
                    var iso = Encoding.GetEncoding("ISO-8859-1");
                    var bytes = iso.GetBytes(doc.OuterXml);
                    st.Write(bytes, 0, bytes.Length);
                }
            }
        }

        private static void Listen()
        {
            var bytes = new byte[1025];
            NetworkStream st = _globTcp.GetStream();
            // Loop to receive all the data sent by the client.
            int i = st.Read(bytes, 0, bytes.Length);
            while ((i != 0))
            {
                // Translate data bytes to a UTF8 string.
                string data = Encoding.UTF8.GetString(bytes, 0, i);
                InterpreteXml(data);
                i = st.Read(bytes, 0, bytes.Length);
            }
        }

        private static void InterpreteXml(string xml)
        {
            _fullXml += xml;
            if (!xml.Contains("</cawpacket>"))
            {
                return;
            }
            using (var reader = new XmlTextReader(new StringReader(_fullXml)))
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.Load(reader);
                var root = xmlDoc.DocumentElement;
                if (root != null)
                {
                    var nodes = root.GetElementsByTagName("module");
                    if (nodes.Count > 0)
                    {
                        var i = 0;
                        foreach (XmlNode node in nodes)
                        {
                            i++;
                            var xmlNode = node.ChildNodes.Item(2);
                            if (xmlNode != null) Console.WriteLine("Dialstatus for module {0}: " + xmlNode.InnerText, i);
                        }
                    }
                }
            }
            _fullXml = "";
        }
    }
}