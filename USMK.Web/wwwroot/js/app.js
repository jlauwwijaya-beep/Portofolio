window.usmk = {
    gulirSoal: function (id) {
        var el = document.getElementById(id);
        if (el) el.scrollIntoView({ behavior: "smooth", block: "center" });
    },

    unduhFile: function (base64, nama) {
        var biner = atob(base64);
        var arr = new Uint8Array(biner.length);
        for (var i = 0; i < biner.length; i++) arr[i] = biner.charCodeAt(i);
        var blob = new Blob([arr], { type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" });
        var url = URL.createObjectURL(blob);
        var a = document.createElement("a");
        a.href = url;
        a.download = nama;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    },

    // Pantauan layar penuh khusus halaman ujian (IsiUjian.razor).
    layar: {
        ref: null,
        _fs: null,
        _vis: null,
        _timer: null,
        _sibuk: false,

        pasang: function (ref) {
            var self = this;
            this.lepas();
            this.ref = ref;
            this._fs = function () {
                if (!self.ref) return;
                if (document.fullscreenElement) {
                    self._kirim("PantauLayarPenuh", true);
                    return;
                }
                // Kalau keluar fullscreen (termasuk Esc), anggap sama dengan keluar layar:
                // kabari .NET agar terhitung sebagai peringatan, lalu coba kembalikan otomatis.
                self._kirim("PantauLayarPenuh", false);
                self._coba(0);
            };
            this._vis = function () {
                if (!self.ref) return;
                self._kirim("PantauVisibilitas", document.hidden);
                // Kembali tampil dari minimize/pindah tab → langsung otomatis layar penuh.
                if (!document.hidden && !document.fullscreenElement)
                    self._coba(0);
            };
            document.addEventListener("fullscreenchange", this._fs);
            document.addEventListener("visibilitychange", this._vis);
        },

        lepas: function () {
            if (this._fs) document.removeEventListener("fullscreenchange", this._fs);
            if (this._vis) document.removeEventListener("visibilitychange", this._vis);
            this._fs = null;
            this._vis = null;
            this.ref = null;
            if (this._timer) { clearTimeout(this._timer); this._timer = null; }
            this._sibuk = false;
        },

        // Coba masuk layar penuh otomatis; n = nomor percobaan (0..2).
        // Kalau semua gagal dan halaman sedang tampil, kabari .NET
        // supaya overlay wajib layar penuh muncul.
        _coba: function (n) {
            var self = this;
            if (!this.ref || this._sibuk || document.fullscreenElement) return;
            this._sibuk = true;

            var selesai = function (ok) {
                self._sibuk = false;
                if (!self.ref || ok || document.fullscreenElement) return;
                // Masih tersembunyi: jangan buang percobaan, tunggu sampai tampil lagi.
                if (document.hidden) return;
                if (n >= 2) { self._kirim("LayarPenuhGagal"); return; }
                self._timer = setTimeout(function () {
                    self._timer = null;
                    self._coba(n + 1);
                }, 600);
            };

            var hasil = null;
            try { hasil = document.documentElement.requestFullscreen(); } catch (e) { }
            if (hasil && hasil.then) {
                hasil.then(function () { selesai(true); }, function () { selesai(false); });
            } else {
                selesai(!!document.fullscreenElement);
            }
        },

        _kirim: function (metode, arg) {
            if (!this.ref) return;
            try {
                if (arguments.length < 2) this.ref.invokeMethodAsync(metode);
                else this.ref.invokeMethodAsync(metode, arg);
            } catch (e) { }
        },

        // Meminta layar penuh. Harus dipanggil dari gestur pengguna (klik)
        // supaya diizinkan browser; hasilnya Promise<boolean>.
        // Dibatasi 2,5 detik supaya promise yang menggantung tidak pernah
        // memblokir kode pemanggil (mis. tombol "Mulai ujian").
        minta: function () {
            var janji;
            try {
                // Sudah layar penuh (mis. masuk dari klik "Mulai"): langsung sukses,
                // jangan request ulang yang bisa ditolak browser tanpa gestur.
                if (document.fullscreenElement) return Promise.resolve(true);
                var p = document.documentElement.requestFullscreen();
                janji = (p && p.then)
                    ? p.then(function () { return true; }, function () { return false; })
                    : Promise.resolve(!!document.fullscreenElement);
            } catch (e) {
                janji = Promise.resolve(false);
            }

            var batasWaktu = new Promise(function (selesai) {
                setTimeout(function () { selesai(!!document.fullscreenElement); }, 2500);
            });

            return Promise.race([janji, batasWaktu]);
        },

        keluar: function () {
            try {
                var p = document.exitFullscreen && document.exitFullscreen();
                if (p && p.then) {
                    return p.then(function () { return true; }, function () { return false; });
                }
            } catch (e) { }
            return Promise.resolve(false);
        },

        aktif: function () {
            return Promise.resolve(!!document.fullscreenElement);
        }
    }
};