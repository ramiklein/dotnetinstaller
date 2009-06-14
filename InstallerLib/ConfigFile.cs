using System;
using System.ComponentModel;
using System.Xml;
using System.Drawing;
using System.Drawing.Design;
using System.ComponentModel.Design;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Reflection;

namespace InstallerLib
{
    public enum InstallUILevel
    {
        /// <summary>
        /// Full install, all UI.
        /// </summary>
        full,
        /// <summary>
        /// Basic UI.
        /// </summary>
        basic,
        /// <summary>
        /// Silent, no UI.
        /// </summary>
        silent
    }

    /// <summary>
    /// A configuration file.
    /// </summary>
    [XmlChild(typeof(WebConfiguration))]
    [XmlChild(typeof(SetupConfiguration))]
    public class ConfigFile : XmlClassImpl
    {
        public ConfigFile()
        {
        }

        private string m_filename = null;
        [Browsable(false)]
        public string filename
        {
            get { return m_filename; }
            set { m_filename = value; }
        }

        private InstallUILevel m_ui_level = InstallUILevel.full;
        [Description("Set the install-time UI level. Default to silent or basic UI installation.")]
        [Category("Installation Runtime")]
        public InstallUILevel ui_level
        {
            get { return m_ui_level; }
            set { m_ui_level = value; }
        }

        private string m_fileversion;
        [Description("Four-part binary file version, eg. 1.2.3.4")]
        [Category("File Attributes")]
        public string fileversion
        {
            get { return m_fileversion; }
            set { m_fileversion = value; }
        }

        private string m_productversion;
        [Description("Four-part binary product version, eg. 1.2.3.4")]
        [Category("File Attributes")]
        public string productversion
        {
            get { return m_productversion; }
            set { m_productversion = value; }
        }

        // message for not matching the OS filter
        private string m_configuration_no_match_message;
        [Description("An error message for the case that the operating system, processor architecture or lcid does not match the operating system filter of any configuration. (OPTIONAL)")]
        [Category("Messages")]
        [Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
        public string configuration_no_match_message
        {
            get { return m_configuration_no_match_message; }
            set { m_configuration_no_match_message = value; }
        }

        private FileAttributeCollection m_fileattributes = new FileAttributeCollection();
        [Description("attributes")]
        [Category("File Attributes")]
        public FileAttributeCollection fileattributes
        {
            get { return m_fileattributes; }
            set { m_fileattributes = value; }
        }

        private LcidType m_lcidtype = LcidType.UserExe;
        [Description(@"Choose between using LCID from system32\user.exe, GetUserDefaultLCID or system GetSystemDefaultLCID.")]
        [Category("Installation Runtime")]
        public LcidType lcidtype
        {
            get { return m_lcidtype; }
            set { m_lcidtype = value; }
        }

        private Editor m_editor = new Editor();
        [Description(@"Editor information.")]
        [Category("Configuration File")]
        public Editor editor
        {
            get { return m_editor; }
        }

        public void Save()
        {
            if (string.IsNullOrEmpty(m_filename))
            {
                throw new Exception("Missing filename");
            }

            SaveAs(m_filename);
        }

        /// <summary>
        /// Save the xml to a file.
        /// </summary>
        /// <param name="p_FileName">target filename</param>
        public void SaveAs(string p_FileName)
        {
            Xml.Save(p_FileName);
            m_filename = p_FileName;
        }

        /// <summary>
        /// Raw configuration xml.
        /// </summary>
        [Browsable(false)]
        public XmlDocument Xml
        {
            get
            {
                MemoryStream ms = new MemoryStream();
                XmlTextWriter l_XmlWriter = new XmlTextWriter(ms, Encoding.UTF8);
                l_XmlWriter.Formatting = Formatting.Indented;
                l_XmlWriter.WriteStartDocument();
                ToXml(l_XmlWriter);
                XmlDocument xmldoc = new XmlDocument();
                l_XmlWriter.Flush();
                ms.Position = 0;
                xmldoc.Load(ms);
                return xmldoc;
            }
        }

        public override string XmlTag
        {
            get { return "configurations"; }
        }

        protected override void OnXmlWriteTag(XmlWriterEventArgs e)
        {
            // lcid type
            e.XmlWriter.WriteAttributeString("lcid_type", m_lcidtype.ToString());
            // processor and os filter architecture messages
            e.XmlWriter.WriteAttributeString("configuration_no_match_message", m_configuration_no_match_message);
            // silent install
            e.XmlWriter.WriteAttributeString("ui_level", m_ui_level.ToString());
            // version information
            e.XmlWriter.WriteAttributeString("fileversion", m_fileversion);
            e.XmlWriter.WriteAttributeString("productversion", m_productversion);
            // tag schema
            editor.ToXml(e.XmlWriter);
            // file attributes
            fileattributes.ToXml(e.XmlWriter);
            base.OnXmlWriteTag(e);
        }

        protected override void OnXmlReadTag(XmlElementEventArgs e)
        {
            // lcid type
            ReadAttributeValue(e, "lcid_type", ref m_lcidtype);
            // processor and os filter architecture messages
            ReadAttributeValue(e, "configuration_no_match_message", ref m_configuration_no_match_message);
            // ui level
            ReadAttributeValue<InstallUILevel>(e, "ui_level", ref m_ui_level);
            // backwards compatibility: silent_install
            bool silentInstall = false;
            if (ReadAttributeValue(e, "silent_install", ref silentInstall) && silentInstall)
            {
                m_ui_level = InstallUILevel.silent;
            }
            // version information
            ReadAttributeValue(e, "fileversion", ref m_fileversion);
            ReadAttributeValue(e, "productversion", ref m_productversion);
            base.OnXmlReadTag(e);
        }

        protected override bool OnXmlChild(XmlElement child)
        {
            bool processed = false;
            switch (child.LocalName)
            {
                case "schema":
                    m_editor = Editor.CreateFromXml(child);
                    processed = true;
                    break;
                case "fileattributes":
                    fileattributes.FromXml(child);
                    processed = true;
                    break;
                case "configurations":
                    FromXml(child);
                    processed = true;
                    break;
                default:
                    break;
            }
            return processed;
        }

        public void Create(string p_FileName)
        {
            m_filename = p_FileName;
        }

        public void Load(string p_FileName)
        {
            XmlDocument l_XmlElement = new XmlDocument();
            l_XmlElement.Load(p_FileName);
            LoadXml(l_XmlElement);
            m_filename = p_FileName;
        }

        public void LoadXml(string xml)
        {
            XmlDocument l_XmlElement = new XmlDocument();
            l_XmlElement.LoadXml(xml);
            LoadXml(l_XmlElement);
        }

        public void LoadXml(XmlDocument xml)
        {
            base.FromXml((XmlElement) xml.SelectSingleNode("//configurations"));
        }
    }
}
