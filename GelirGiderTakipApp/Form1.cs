using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace GelirGiderTakip
{
    public enum IslemTipi
    {
        Gelir,
        Gider
    }

    public class FinansIslemi
    {
        public int Id { get; set; }
        public string Baslik { get; set; }
        public decimal Tutar { get; set; }
        public IslemTipi Tip { get; set; }
        public string Kategori { get; set; }
        public DateTime Tarih { get; set; }
    }

    public static class DatabaseHelper
    {
        private static string connectionString = "Server=localhost;Database=finans_db;Uid=root;Pwd=1234;";

        public static MySqlConnection GetConnection()
        {
            return new MySqlConnection(connectionString);
        }

        public static List<FinansIslemi> IslemleriGetir()
        {
            var liste = new List<FinansIslemi>();
            string query = "SELECT Id, Baslik, Tutar, Tip, Kategori, Tarih FROM islemler ORDER BY Tarih DESC";

            using (var conn = GetConnection())
            {
                using (var cmd = new MySqlCommand(query, conn))
                {
                    try
                    {
                        conn.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                liste.Add(new FinansIslemi
                                {
                                    Id = reader.GetInt32("Id"),
                                    Baslik = reader.GetString("Baslik"),
                                    Tutar = reader.GetDecimal("Tutar"),
                                    Tip = reader.GetString("Tip") == "Gelir" ? IslemTipi.Gelir : IslemTipi.Gider,
                                    Kategori = reader.GetString("Kategori"),
                                    Tarih = reader.GetDateTime("Tarih")
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Veritabanı bağlantı hatası: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            return liste;
        }

        public static void IslemEkle(FinansIslemi islem)
        {
            string query = "INSERT INTO islemler (Baslik, Tutar, Tip, Kategori, Tarih) VALUES (@baslik, @tutar, @tip, @kategori, @tarih)";
            using (var conn = GetConnection())
            using (var cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@baslik", islem.Baslik);
                cmd.Parameters.AddWithValue("@tutar", islem.Tutar);
                cmd.Parameters.AddWithValue("@tip", islem.Tip.ToString());
                cmd.Parameters.AddWithValue("@kategori", islem.Kategori);
                cmd.Parameters.AddWithValue("@tarih", islem.Tarih);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public static void IslemGuncelle(FinansIslemi islem)
        {
            string query = "UPDATE islemler SET Baslik = @baslik, Tutar = @tutar, Tip = @tip, Kategori = @kategori, Tarih = @tarih WHERE Id = @id";
            using (var conn = GetConnection())
            using (var cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@id", islem.Id);
                cmd.Parameters.AddWithValue("@baslik", islem.Baslik);
                cmd.Parameters.AddWithValue("@tutar", islem.Tutar);
                cmd.Parameters.AddWithValue("@tip", islem.Tip.ToString());
                cmd.Parameters.AddWithValue("@kategori", islem.Kategori);
                cmd.Parameters.AddWithValue("@tarih", islem.Tarih);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public static void IslemSil(int id)
        {
            string query = "DELETE FROM islemler WHERE Id = @id";
            using (var conn = GetConnection())
            using (var cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }

    // Modern Yuvarlatılmış Köşeli Buton Sınıfı
    public class RoundedButton : Button
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(0, 0, this.Width, this.Height);
            using (GraphicsPath path = GetRoundedRectPath(rect, 8))
            {
                this.Region = new Region(path);
                using (SolidBrush brush = new SolidBrush(this.BackColor))
                {
                    e.Graphics.FillPath(brush, path);
                }
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    using (SolidBrush textBrush = new SolidBrush(this.ForeColor))
                    {
                        e.Graphics.DrawString(this.Text, this.Font, textBrush, rect, sf);
                    }
                }
            }
        }

        private GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    public partial class Form1 : Form
    {
        private DataGridView dgvIslemler;
        private TextBox txtBaslik, txtTutar;
        private ComboBox cmbKategori, cmbFiltre;
        private DateTimePicker dtpTarih;
        private RadioButton rdbGelir, rdbGider;
        private RoundedButton btnKaydet, btnGuncelle, btnSil, btnTemizle;
        private Label lblToplamGelir, lblToplamGider, lblBakiye;

        private List<FinansIslemi> tumIslemler = new List<FinansIslemi>();
        private int? aktifGuncellemeId = null;

        // İstediğiniz Renk Paleti
        private readonly Color cKoyuLacivert = ColorTranslator.FromHtml("#071B2B");
        private readonly Color cPetrolMavisi = ColorTranslator.FromHtml("#167D8D");
        private readonly Color cBuzMavisi = ColorTranslator.FromHtml("#B9E4E8");
        private readonly Color cGriBeyaz = ColorTranslator.FromHtml("#EEEEEE");

        public Form1()
        {
            ArayuzuOlustur();
            ModernTemayiUygula();
            VerileriYukle();
        }

        private void ArayuzuOlustur()
        {
            this.Text = "Gelir Gider Takip";
            this.Size = new Size(1280, 760);
            this.MinimumSize = new Size(1280, 760);
            this.MaximumSize = new Size(1280, 760); // Sabit Boyut (Max Size)
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = cGriBeyaz;

            // 1. SOL MENÜ / PANEL (Form Giriş Alanı)
            Panel pnlSol = new Panel { Dock = DockStyle.Left, Width = 340, BackColor = cKoyuLacivert, Padding = new Padding(20) };

            Label lblBaslikHeader = new Label { Text = "GELİR GİDER TAKİP", Top = 20, Left = 20, AutoSize = true, Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = cBuzMavisi };

            Label lbl1 = new Label { Text = "İşlem Başlığı / Açıklama", Top = 70, Left = 20, AutoSize = true, ForeColor = cBuzMavisi };
            txtBaslik = new TextBox { Top = 95, Left = 20, Width = 290, Height = 28, Font = new Font("Segoe UI", 10) };

            Label lbl2 = new Label { Text = "Tutar (₺)", Top = 140, Left = 20, AutoSize = true, ForeColor = cBuzMavisi };
            txtTutar = new TextBox { Top = 165, Left = 20, Width = 290, Height = 28, Font = new Font("Segoe UI", 10) };

            Label lbl3 = new Label { Text = "İşlem Türü", Top = 210, Left = 20, AutoSize = true, ForeColor = cBuzMavisi };
            rdbGelir = new RadioButton { Text = "Gelir", Top = 235, Left = 20, Width = 90, ForeColor = cGriBeyaz, Font = new Font("Segoe UI", 10) };
            rdbGider = new RadioButton { Text = "Gider", Top = 235, Left = 120, Width = 90, Checked = true, ForeColor = cGriBeyaz, Font = new Font("Segoe UI", 10) };

            Label lbl4 = new Label { Text = "Kategori", Top = 280, Left = 20, AutoSize = true, ForeColor = cBuzMavisi };
            cmbKategori = new ComboBox { Top = 305, Left = 20, Width = 290, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10) };
            cmbKategori.Items.AddRange(new string[] { "Genel", "Maaş / Ek Gelir", "Kira", "Fatura (Elektrik/Su/Net)", "Market & Mutfak", "Ulaşım", "Eğlence", "Yatırım" });
            cmbKategori.SelectedIndex = 0;

            Label lbl5 = new Label { Text = "İşlem Tarihi", Top = 350, Left = 20, AutoSize = true, ForeColor = cBuzMavisi };
            dtpTarih = new DateTimePicker { Top = 375, Left = 20, Width = 290, Format = DateTimePickerFormat.Short, Font = new Font("Segoe UI", 10) };

            // Yuvarlatılmış Aksiyon Butonları
            btnKaydet = new RoundedButton { Text = "＋ Yeni İşlem Kaydet", Top = 430, Left = 20, Width = 290, Height = 42, Cursor = Cursors.Hand };
            btnKaydet.Click += btnKaydet_Click;

            btnGuncelle = new RoundedButton { Text = "✎ Seçileni Güncelle", Top = 480, Left = 20, Width = 290, Height = 40, Cursor = Cursors.Hand, Enabled = false };
            btnGuncelle.Click += btnGuncelle_Click;

            btnSil = new RoundedButton { Text = "Sil", Top = 530, Left = 20, Width = 140, Height = 36, Cursor = Cursors.Hand };
            btnSil.Click += btnSil_Click;

            btnTemizle = new RoundedButton { Text = "Temizle", Top = 530, Left = 170, Width = 140, Height = 36, Cursor = Cursors.Hand };
            btnTemizle.Click += (s, e) => FormuSifirla();

            pnlSol.Controls.AddRange(new Control[] { lblBaslikHeader, lbl1, txtBaslik, lbl2, txtTutar, lbl3, rdbGelir, rdbGider, lbl4, cmbKategori, lbl5, dtpTarih, btnKaydet, btnGuncelle, btnSil, btnTemizle });

            // 2. ÜST ÖZET KARTLARI PANELI
            Panel pnlUst = new Panel { Dock = DockStyle.Top, Height = 100, BackColor = Color.White };
            lblToplamGelir = OzetKartiOlustur(pnlUst, "TOPLAM GELİR", "0.00 ₺", 30);
            lblToplamGider = OzetKartiOlustur(pnlUst, "TOPLAM GİDER", "0.00 ₺", 280);
            lblBakiye = OzetKartiOlustur(pnlUst, "NET BAKİYE", "0.00 ₺", 530);

            // 3. SAĞ ANA İÇERİK PANELI (Tablo ve Filtre)
            Panel pnlSag = new Panel { Dock = DockStyle.Fill, Padding = new Padding(25), BackColor = cGriBeyaz };

            Label lblFiltre = new Label { Text = "Kategoriye Göre Filtrele:", Top = 18, Left = 20, AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = cKoyuLacivert };
            cmbFiltre = new ComboBox { Top = 15, Left = 190, Width = 220, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10) };
            cmbFiltre.Items.Add("Tümünü Göster");
            cmbFiltre.Items.AddRange(cmbKategori.Items.Cast<object>().ToArray());
            cmbFiltre.SelectedIndex = 0;
            cmbFiltre.SelectedIndexChanged += (s, e) => FiltreleVeGoster();

            dgvIslemler = new DataGridView
            {
                Top = 60,
                Left = 20,
                Width = 860,
                Height = 530,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = true,
                AllowUserToAddRows = false,
                RowTemplate = { Height = 35 }
            };
            dgvIslemler.CellDoubleClick += DgvIslemler_CellDoubleClick;

            pnlSag.Controls.AddRange(new Control[] { lblFiltre, cmbFiltre, dgvIslemler });

            this.Controls.Add(pnlSag);
            this.Controls.Add(pnlUst);
            this.Controls.Add(pnlSol);
        }

        private Label OzetKartiOlustur(Panel parent, string baslik, string deger, int left)
        {
            Panel kart = new Panel { Top = 15, Left = left, Width = 230, Height = 70, BackColor = cBuzMavisi };

            Label lblBaslik = new Label { Text = baslik, Top = 12, Left = 15, AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = cKoyuLacivert };
            Label lblDeger = new Label { Text = deger, Top = 35, Left = 15, AutoSize = true, Font = new Font("Segoe UI", 13, FontStyle.Bold), ForeColor = cKoyuLacivert };

            kart.Controls.Add(lblBaslik);
            kart.Controls.Add(lblDeger);
            parent.Controls.Add(kart);
            return lblDeger;
        }

        private void ModernTemayiUygula()
        {
            dgvIslemler.BackgroundColor = Color.White;
            dgvIslemler.BorderStyle = BorderStyle.None;
            dgvIslemler.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvIslemler.DefaultCellStyle.BackColor = Color.White;
            dgvIslemler.DefaultCellStyle.ForeColor = cKoyuLacivert;
            dgvIslemler.DefaultCellStyle.Font = new Font("Segoe UI", 10);
            dgvIslemler.DefaultCellStyle.SelectionBackColor = cPetrolMavisi;
            dgvIslemler.DefaultCellStyle.SelectionForeColor = Color.White;

            dgvIslemler.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgvIslemler.ColumnHeadersDefaultCellStyle.BackColor = cKoyuLacivert;
            dgvIslemler.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvIslemler.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            dgvIslemler.ColumnHeadersHeight = 40;
            dgvIslemler.EnableHeadersVisualStyles = false;

            StilAyarla(btnKaydet, cPetrolMavisi, Color.White);
            StilAyarla(btnGuncelle, ColorTranslator.FromHtml("#D97706"), Color.White);
            StilAyarla(btnSil, ColorTranslator.FromHtml("#DC2626"), Color.White);
            StilAyarla(btnTemizle, ColorTranslator.FromHtml("#4B5563"), Color.White);
        }

        private void StilAyarla(RoundedButton btn, Color bg, Color fg)
        {
            btn.BackColor = bg;
            btn.ForeColor = fg;
            btn.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
        }

        private void VerileriYukle()
        {
            tumIslemler = DatabaseHelper.IslemleriGetir();
            FiltreleVeGoster();
        }

        private void FiltreleVeGoster()
        {
            var filtrelenmis = tumIslemler.AsEnumerable();

            if (cmbFiltre.SelectedIndex > 0)
            {
                string secilenKat = cmbFiltre.SelectedItem.ToString();
                filtrelenmis = filtrelenmis.Where(x => x.Kategori == secilenKat);
            }

            dgvIslemler.DataSource = null;
            dgvIslemler.DataSource = filtrelenmis.ToList();

            // ID Sütununu Gizle
            if (dgvIslemler.Columns["Id"] != null)
            {
                dgvIslemler.Columns["Id"].Visible = false;
            }

            decimal toplamGelir = tumIslemler.Where(x => x.Tip == IslemTipi.Gelir).Sum(x => x.Tutar);
            decimal toplamGider = tumIslemler.Where(x => x.Tip == IslemTipi.Gider).Sum(x => x.Tutar);
            decimal bakiye = toplamGelir - toplamGider;

            lblToplamGelir.Text = $"{toplamGelir:N2} ₺";
            lblToplamGider.Text = $"{toplamGider:N2} ₺";
            lblBakiye.Text = $"{bakiye:N2} ₺";
        }

        private void btnKaydet_Click(object sender, EventArgs e)
        {
            if (!FormuDogrula()) return;

            var yeniIslem = new FinansIslemi
            {
                Baslik = txtBaslik.Text.Trim(),
                Tutar = decimal.Parse(txtTutar.Text),
                Tip = rdbGelir.Checked ? IslemTipi.Gelir : IslemTipi.Gider,
                Kategori = cmbKategori.SelectedItem.ToString(),
                Tarih = dtpTarih.Value
            };

            DatabaseHelper.IslemEkle(yeniIslem);
            VerileriYukle();
            FormuSifirla();
            MessageBox.Show("İşlem başarıyla kaydedildi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void DgvIslemler_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var secili = (FinansIslemi)dgvIslemler.Rows[e.RowIndex].DataBoundItem;

                aktifGuncellemeId = secili.Id;
                txtBaslik.Text = secili.Baslik;
                txtTutar.Text = secili.Tutar.ToString();

                if (secili.Tip == IslemTipi.Gelir) rdbGelir.Checked = true;
                else rdbGider.Checked = true;

                cmbKategori.SelectedItem = secili.Kategori;
                dtpTarih.Value = secili.Tarih;

                btnKaydet.Enabled = false;
                btnGuncelle.Enabled = true;
                btnGuncelle.BackColor = ColorTranslator.FromHtml("#F59E0B");
            }
        }

        private void btnGuncelle_Click(object sender, EventArgs e)
        {
            if (!aktifGuncellemeId.HasValue || !FormuDogrula()) return;

            var guncelIslem = new FinansIslemi
            {
                Id = aktifGuncellemeId.Value,
                Baslik = txtBaslik.Text.Trim(),
                Tutar = decimal.Parse(txtTutar.Text),
                Tip = rdbGelir.Checked ? IslemTipi.Gelir : IslemTipi.Gider,
                Kategori = cmbKategori.SelectedItem.ToString(),
                Tarih = dtpTarih.Value
            };

            DatabaseHelper.IslemGuncelle(guncelIslem);
            VerileriYukle();
            FormuSifirla();
            MessageBox.Show("İşlem başarıyla güncellendi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnSil_Click(object sender, EventArgs e)
        {
            if (dgvIslemler.SelectedRows.Count > 0)
            {
                var secili = (FinansIslemi)dgvIslemler.SelectedRows[0].DataBoundItem;

                var sonuc = MessageBox.Show($"'{secili.Baslik}' başlıklı kaydı silmek istediğinize emin misiniz?", "Silme Onayı", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (sonuc == DialogResult.Yes)
                {
                    DatabaseHelper.IslemSil(secili.Id);
                    VerileriYukle();
                    FormuSifirla();
                }
            }
            else
            {
                MessageBox.Show("Lütfen silmek için tablodan bir satır seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private bool FormuDogrula()
        {
            if (string.IsNullOrWhiteSpace(txtBaslik.Text))
            {
                MessageBox.Show("Lütfen geçerli bir başlık veya açıklama yazın.", "Eksik Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtBaslik.Focus();
                return false;
            }
            if (!decimal.TryParse(txtTutar.Text, out decimal tutar) || tutar <= 0)
            {
                MessageBox.Show("Lütfen geçerli pozitif bir tutar giriniz.", "Hatalı Tutar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtTutar.Focus();
                return false;
            }
            return true;
        }

        private void FormuSifirla()
        {
            txtBaslik.Clear();
            txtTutar.Clear();
            cmbKategori.SelectedIndex = 0;
            rdbGider.Checked = true;
            dtpTarih.Value = DateTime.Now;

            aktifGuncellemeId = null;
            btnKaydet.Enabled = true;
            btnGuncelle.Enabled = false;
            StilAyarla(btnGuncelle, ColorTranslator.FromHtml("#D97706"), Color.White);
        }
    }
}