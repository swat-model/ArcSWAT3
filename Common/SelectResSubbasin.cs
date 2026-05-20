using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ArcSWAT3
{
    public partial class SelectResSubbasin : Form
    {
        private DelinForm _parent;
        public SelectResSubbasin(DelinForm parent) {
            InitializeComponent();
            this._parent = parent;
        }

        private async void saveButton_Click(object sender, EventArgs e) {
            Close();
            await this._parent.selectResSubbasins(true);
        }

        private async void cancelButton_Click(object sender, EventArgs e) {
            Close();
            await this._parent.selectResSubbasins(false);
        }
    }
}
