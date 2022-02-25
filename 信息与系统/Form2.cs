using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace jiemian2
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        private void confirm_Click(object sender, EventArgs e)
        {
            MySqlConnection conn = new MySqlConnection("Server=localhost;Database=erjixm;uid=root;pwd=Xp344605");

            MySqlCommand cmd = null;

            string cmdString = "";

            conn.Open();

            cmdString = "insert into operator values ('" + textBox1.Text + "','" + textBox2.Text + "','" + textBox3.Text + "','" + textBox4.Text + "');";

            cmd = new MySqlCommand(cmdString, conn);

            cmd.ExecuteNonQuery();

            conn.Close();

            MessageBox.Show("Data Stored Successfully");
        }


    }
}
