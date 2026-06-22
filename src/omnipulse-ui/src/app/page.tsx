"use client";

import React, { useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import {
  Zap,
  Cpu,
  Database,
  ArrowRight,
  ShieldAlert,
  Server,
  Activity,
  Layers,
  CheckCircle,
  HelpCircle,
  Menu,
  X
} from "lucide-react";

export default function LandingPage() {
  const router = useRouter();
  const [user, setUser] = useState<{ email: string; tenantIdentifier: string } | null>(null);
  const [loading, setLoading] = useState(true);
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);

  useEffect(() => {
    const checkAuth = async () => {
      try {
        const res = await fetch("/api/bff/user");
        if (res.ok) {
          const data = await res.json();
          if (data.isAuthenticated) {
            setUser({
              email: data.email,
              tenantIdentifier: data.tenantIdentifier
            });
          }
        }
      } catch (err) {
        console.error("Auth check failed:", err);
      } finally {
        setLoading(false);
      }
    };
    checkAuth();
  }, []);

  const handleDashboardRedirect = () => {
    router.push("/dashboard");
  };

  const handleDemoRedirect = () => {
    router.push("/dashboard?guest=true");
  };

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 font-sans selection:bg-teal-500 selection:text-slate-950 relative overflow-hidden">
      {/* Background Glows */}
      <div className="absolute top-0 right-1/4 w-[600px] h-[600px] bg-teal-500/10 blur-[180px] pointer-events-none rounded-full" />
      <div className="absolute bottom-10 left-10 w-[500px] h-[500px] bg-indigo-500/5 blur-[150px] pointer-events-none rounded-full" />

      {/* Navigation Header */}
      <nav className="border-b border-slate-900 bg-slate-950/70 backdrop-blur-md sticky top-0 z-50 px-6 py-4 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="p-2 rounded-lg bg-gradient-to-tr from-teal-500 to-indigo-500 text-slate-950">
            <Zap className="w-6 h-6" />
          </div>
          <div>
            <span className="text-lg font-black tracking-wider bg-clip-text text-transparent bg-gradient-to-r from-white to-slate-400">
              OMNIPULSE
            </span>
          </div>
        </div>

        {/* Desktop Menu */}
        <div className="hidden md:flex items-center gap-8 text-xs font-bold uppercase tracking-wider text-slate-400">
          <a href="#features" className="hover:text-teal-400 transition-colors">Özellikler</a>
          <a href="#architecture" className="hover:text-teal-400 transition-colors">Mimari</a>
          <a href="#pricing" className="hover:text-teal-400 transition-colors">Fiyatlandırma</a>
          <div className="flex items-center gap-1.5 px-3 py-1 rounded bg-emerald-500/10 border border-emerald-500/20 text-emerald-400 font-mono text-[10px]">
            <span className="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-pulse" />
            STABIL
          </div>
        </div>

        {/* Action Buttons */}
        <div className="hidden md:flex items-center gap-4">
          <button
            onClick={handleDemoRedirect}
            className="px-4 py-2 rounded-xl border border-slate-800 bg-slate-900/60 hover:bg-slate-800 text-slate-300 text-xs font-bold transition-all duration-200 active:scale-95 cursor-pointer"
          >
            Canlı Demo İzle
          </button>
          
          {loading ? (
            <div className="w-20 h-8 rounded-xl bg-slate-900 animate-pulse" />
          ) : user ? (
            <button
              onClick={handleDashboardRedirect}
              className="flex items-center gap-1 px-4 py-2 rounded-xl bg-gradient-to-r from-teal-500 to-indigo-500 text-slate-950 hover:shadow-lg hover:shadow-teal-500/20 font-bold text-xs transition-all duration-200 active:scale-95 cursor-pointer"
            >
              Konsol &rarr;
            </button>
          ) : (
            <button
              onClick={handleDashboardRedirect}
              className="flex items-center gap-1 px-4 py-2 rounded-xl bg-teal-500 hover:bg-teal-400 text-slate-950 font-bold text-xs transition-all duration-200 active:scale-95 cursor-pointer"
            >
              Giriş Yap
            </button>
          )}
        </div>

        {/* Mobile Menu Button */}
        <button
          onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
          className="md:hidden p-2 rounded-lg bg-slate-900 border border-slate-800 text-slate-300 cursor-pointer"
        >
          {mobileMenuOpen ? <X className="w-5 h-5" /> : <Menu className="w-5 h-5" />}
        </button>
      </nav>

      {/* Mobile Dropdown Menu */}
      {mobileMenuOpen && (
        <div className="md:hidden fixed top-[69px] left-0 right-0 bg-slate-950 border-b border-slate-900 z-40 p-6 space-y-4 animate-fade-in">
          <div className="flex flex-col space-y-3 text-sm font-bold uppercase tracking-wider text-slate-400">
            <a href="#features" onClick={() => setMobileMenuOpen(false)} className="hover:text-teal-400">Özellikler</a>
            <a href="#architecture" onClick={() => setMobileMenuOpen(false)} className="hover:text-teal-400">Mimari</a>
            <a href="#pricing" onClick={() => setMobileMenuOpen(false)} className="hover:text-teal-400">Fiyatlandırma</a>
          </div>
          <div className="pt-4 border-t border-slate-900 flex flex-col gap-3">
            <button
              onClick={() => { setMobileMenuOpen(false); handleDemoRedirect(); }}
              className="w-full py-2.5 rounded-xl border border-slate-800 bg-slate-900 text-slate-300 text-xs font-bold"
            >
              Canlı Demo İzle
            </button>
            <button
              onClick={() => { setMobileMenuOpen(false); handleDashboardRedirect(); }}
              className="w-full py-2.5 rounded-xl bg-teal-500 text-slate-950 text-xs font-bold"
            >
              {user ? "Yönetim Konsolu" : "Giriş Yap"}
            </button>
          </div>
        </div>
      )}

      {/* Hero Section */}
      <section className="relative px-6 py-20 md:py-32 flex flex-col items-center justify-center text-center space-y-8 max-w-4xl mx-auto">
        <div className="inline-flex items-center gap-2 px-3 py-1.5 rounded-full border border-teal-500/30 bg-teal-500/5 text-teal-400 text-xs font-mono tracking-wide uppercase animate-pulse">
          <Activity className="w-3.5 h-3.5" />
          Endüstriyel IoT & SIEM Telemetri Akış Platformu
        </div>

        <h1 className="text-4xl sm:text-6xl font-black tracking-tight leading-tight bg-clip-text text-transparent bg-gradient-to-r from-white via-slate-100 to-slate-500">
          Tüm Sensörlerinizi <br className="hidden sm:block" />
          <span className="bg-clip-text text-transparent bg-gradient-to-r from-teal-400 to-indigo-400">
            Tek Bir Çatı Altında
          </span> İzleyin
        </h1>

        <p className="text-sm sm:text-base text-slate-400 leading-relaxed max-w-2xl">
          AWS Kinesis veri hattı, PostgreSQL çoklu kiracılık (Multi-Tenancy) altyapısı ve SignalR gerçek zamanlı soket akışı ile donatılmış, siber güvenlik odaklı IoT yönetim merkezi.
        </p>

        <div className="flex flex-col sm:flex-row items-center gap-4 pt-4">
          <button
            onClick={handleDemoRedirect}
            className="flex items-center gap-2 px-8 py-4 rounded-xl bg-gradient-to-r from-teal-500 to-indigo-500 text-slate-950 font-black text-sm tracking-wider shadow-xl hover:shadow-teal-500/10 hover:from-teal-400 hover:to-indigo-400 hover:scale-[1.02] active:scale-98 transition-all duration-200 cursor-pointer"
          >
            Hemen Canlı Demoyu İncele ⚡
          </button>
          <button
            onClick={() => router.push("/register")}
            className="flex items-center gap-1.5 px-8 py-4 rounded-xl border border-slate-800 bg-slate-900/40 hover:bg-slate-900 text-slate-300 font-bold text-sm tracking-wide transition-all active:scale-98 cursor-pointer"
          >
            Şirket Kaydı Oluştur
            <ArrowRight className="w-4 h-4" />
          </button>
        </div>
      </section>

      {/* Grid Dashboard Mock Preview */}
      <section className="px-6 pb-24 max-w-6xl mx-auto z-10 relative">
        <div className="p-1.5 rounded-3xl bg-slate-900/30 border border-slate-800/80 backdrop-blur-xl shadow-2xl overflow-hidden group">
          <div className="bg-slate-950 rounded-2xl border border-slate-900 p-4 sm:p-6 space-y-6">
            {/* Mock Header */}
            <div className="flex items-center justify-between border-b border-slate-900 pb-4">
              <div className="flex items-center gap-2.5">
                <span className="w-3 h-3 rounded-full bg-rose-500 animate-ping" />
                <span className="text-xs font-mono text-slate-400">Canlı Sistem Simülasyonu</span>
              </div>
              <div className="text-[10px] font-mono bg-indigo-950/40 text-indigo-400 border border-indigo-500/20 px-2 py-0.5 rounded">
                AWS KINESIS ACTIVE
              </div>
            </div>
            
            {/* Mock Grid */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              <div className="p-4 rounded-xl bg-slate-900/40 border border-slate-800/60 font-mono text-xs space-y-3">
                <div className="flex items-center justify-between text-slate-500">
                  <span>[SN-TEMP-DEMO]</span>
                  <span className="text-emerald-400">ONLINE</span>
                </div>
                <div className="text-sm font-bold text-slate-200">Sıcaklık Sensörü 01</div>
                <div className="text-2xl font-black text-teal-400">4.8 °C</div>
              </div>
              
              <div className="p-4 rounded-xl bg-slate-900/40 border border-slate-800/60 font-mono text-xs space-y-3">
                <div className="flex items-center justify-between text-slate-500">
                  <span>[SN-VIB-DEMO]</span>
                  <span className="text-emerald-400">ONLINE</span>
                </div>
                <div className="text-sm font-bold text-slate-200">Bant Titreşim Analizi</div>
                <div className="text-2xl font-black text-indigo-400">76.3 Hz</div>
              </div>

              <div className="p-4 rounded-xl bg-slate-900/40 border border-slate-800/60 font-mono text-xs space-y-3 md:col-span-2 lg:col-span-1">
                <div className="flex items-center justify-between text-slate-500">
                  <span>[SN-PRES-DEMO]</span>
                  <span className="text-emerald-400">ONLINE</span>
                </div>
                <div className="text-sm font-bold text-slate-200">Pnömatik Hat Basıncı</div>
                <div className="text-2xl font-black text-amber-500">128 kPa</div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Features Section */}
      <section id="features" className="px-6 py-24 border-t border-slate-900 bg-slate-950/30 relative">
        <div className="max-w-6xl mx-auto space-y-16">
          <div className="text-center space-y-4 max-w-2xl mx-auto">
            <h2 className="text-2xl sm:text-4xl font-extrabold tracking-tight text-slate-100">
              Modern Endüstri İçin Tasarlanmış Özellikler
            </h2>
            <p className="text-xs sm:text-sm text-slate-400">
              Platformumuz veri güvenliği, veri akışı ve esnek mimari prensipleri gözetilerek sıfırdan inşa edilmiştir.
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
            <div className="p-6 rounded-2xl bg-slate-900/30 border border-slate-800/60 space-y-4">
              <div className="p-2.5 rounded-xl bg-teal-500/10 border border-teal-500/20 text-teal-400 w-fit">
                <Layers className="w-6 h-6" />
              </div>
              <h3 className="text-base font-bold text-slate-200">Çoklu Kiracılık (Multi-Tenancy)</h3>
              <p className="text-xs text-slate-400 leading-relaxed">
                %100 izole edilmiş veritabanı bağlantılarıyla (BFF entegrasyonlu), şirketinizin verilerini diğer kiracılardan bağımsız olarak güvende tutar.
              </p>
            </div>

            <div className="p-6 rounded-2xl bg-slate-900/30 border border-slate-800/60 space-y-4">
              <div className="p-2.5 rounded-xl bg-rose-500/10 border border-rose-500/20 text-rose-400 w-fit">
                <ShieldAlert className="w-6 h-6" />
              </div>
              <h3 className="text-base font-bold text-slate-200">SIEM Güvenlik Kalkanı</h3>
              <p className="text-xs text-slate-400 leading-relaxed">
                Sisteme kayıtlı olmayan veya pasif durumdaki cihazların telemetri sızdırma girişimlerini anında tespit edip SIEM alarmları üretir.
              </p>
            </div>

            <div className="p-6 rounded-2xl bg-slate-900/30 border border-slate-800/60 space-y-4">
              <div className="p-2.5 rounded-xl bg-indigo-500/10 border border-indigo-500/20 text-indigo-400 w-fit">
                <Activity className="w-6 h-6" />
              </div>
              <h3 className="text-base font-bold text-slate-200">SignalR Canlı Akış</h3>
              <p className="text-xs text-slate-400 leading-relaxed">
                Kinesis'ten beslenen telemetri kuyruğunu web-socket mimarisiyle tarayıcınıza milisaniyeler içerisinde, ekranı yenilemeden taşır.
              </p>
            </div>

            <div className="p-6 rounded-2xl bg-slate-900/30 border border-slate-800/60 space-y-4">
              <div className="p-2.5 rounded-xl bg-amber-500/10 border border-amber-500/20 text-amber-400 w-fit">
                <Cpu className="w-6 h-6" />
              </div>
              <h3 className="text-base font-bold text-slate-200">Varlık Hiyerarşisi (Assets)</h3>
              <p className="text-xs text-slate-400 leading-relaxed">
                Lojistik, Fabrika gibi sektör fark etmeksizin; tırları, bantları veya tesisleri ağaç (Parent-Child) yapısında modellemenizi sağlar.
              </p>
            </div>
          </div>
        </div>
      </section>

      {/* Architecture Pipeline Section */}
      <section id="architecture" className="px-6 py-24 border-t border-slate-900 relative">
        <div className="max-w-6xl mx-auto space-y-12">
          <div className="text-center space-y-4 max-w-2xl mx-auto">
            <h2 className="text-2xl sm:text-4xl font-extrabold tracking-tight text-slate-100">
              Veri Akışı ve Pipeline Mimarisi
            </h2>
            <p className="text-xs sm:text-sm text-slate-400">
              Uçtan uca düşük gecikmeli, SIEM destekli ve veri tutarlılığı yüksek veri yolu (pipeline) altyapısı.
            </p>
          </div>

          <div className="p-6 sm:p-8 rounded-3xl bg-slate-900/20 border border-slate-800/80 backdrop-blur-md">
            <div className="grid grid-cols-1 md:grid-cols-5 gap-4 items-center">
              <div className="p-4 rounded-xl bg-slate-950 border border-slate-900 text-center font-mono text-xs">
                <div className="text-teal-400 font-bold mb-1">Cihazlar / Uç Noktalar</div>
                <span>IoT Sensörleri</span>
              </div>
              <div className="text-center text-slate-600 font-bold rotate-90 md:rotate-0">&rarr;</div>
              
              <div className="p-4 rounded-xl bg-slate-950 border border-slate-900 text-center font-mono text-xs">
                <div className="text-indigo-400 font-bold mb-1">API & Outbox</div>
                <span>C# .NET API & PostgreSQL</span>
              </div>
              <div className="text-center text-slate-600 font-bold rotate-90 md:rotate-0">&rarr;</div>

              <div className="p-4 rounded-xl bg-slate-950 border border-slate-900 text-center font-mono text-xs col-span-1 md:col-span-1">
                <div className="text-rose-400 font-bold mb-1">Akış (Kinesis / SIEM)</div>
                <span>AWS Kinesis & Alarmlar</span>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Pricing Section */}
      <section id="pricing" className="px-6 py-24 border-t border-slate-900 bg-slate-950/30 relative">
        <div className="max-w-6xl mx-auto space-y-16">
          <div className="text-center space-y-4 max-w-2xl mx-auto">
            <h2 className="text-2xl sm:text-4xl font-extrabold tracking-tight text-slate-100">
              Esnek Abonelik Modelleri
            </h2>
            <p className="text-xs sm:text-sm text-slate-400">
              14 günlük deneme sürümümüzle anında başlayın. Kurulum ücreti yok, gizli ücretler yok.
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-8 max-w-4xl mx-auto">
            {/* Free Trial */}
            <div className="p-6 rounded-2xl bg-slate-900/30 border border-slate-800/60 flex flex-col justify-between space-y-6">
              <div className="space-y-4">
                <div className="text-xs font-mono text-teal-400 font-bold uppercase">Başlangıç</div>
                <h3 className="text-xl font-bold text-slate-200">14-Günlük Deneme</h3>
                <div className="text-3xl font-black text-slate-100 font-mono">0 $</div>
                <ul className="text-xs text-slate-400 space-y-2.5 pt-4 border-t border-slate-800/60 font-mono">
                  <li className="flex items-center gap-2"><CheckCircle className="w-3.5 h-3.5 text-teal-400" /> 3 Sensör Envanteri</li>
                  <li className="flex items-center gap-2"><CheckCircle className="w-3.5 h-3.5 text-teal-400" /> Demo Simülasyonu</li>
                  <li className="flex items-center gap-2"><CheckCircle className="w-3.5 h-3.5 text-teal-400" /> SignalR Soket Akışı</li>
                </ul>
              </div>
              <button
                onClick={() => router.push("/register")}
                className="w-full py-2.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-bold transition-all cursor-pointer"
              >
                Ücretsiz Denemeyi Başlat
              </button>
            </div>

            {/* Pro Plan */}
            <div className="p-6 rounded-2xl bg-gradient-to-b from-indigo-950/20 via-slate-900/30 to-slate-900/30 border border-indigo-500/40 relative flex flex-col justify-between space-y-6 shadow-xl shadow-indigo-500/5">
              <div className="absolute top-0 right-6 -translate-y-1/2 px-3 py-1 rounded-full bg-indigo-500 text-slate-950 text-[10px] font-black tracking-wide uppercase">
                EN POPÜLER
              </div>
              <div className="space-y-4">
                <div className="text-xs font-mono text-indigo-400 font-bold uppercase">Profesyonel</div>
                <h3 className="text-xl font-bold text-slate-200">Endüstriyel Paket</h3>
                <div className="text-3xl font-black text-slate-100 font-mono">149 $ <span className="text-xs font-normal text-slate-500">/ ay</span></div>
                <ul className="text-xs text-slate-400 space-y-2.5 pt-4 border-t border-slate-800/60 font-mono">
                  <li className="flex items-center gap-2"><CheckCircle className="w-3.5 h-3.5 text-indigo-400" /> Sınırsız Sensör Bağlantısı</li>
                  <li className="flex items-center gap-2"><CheckCircle className="w-3.5 h-3.5 text-indigo-400" /> E-Posta Davet Sistemi</li>
                  <li className="flex items-center gap-2"><CheckCircle className="w-3.5 h-3.5 text-indigo-400" /> SIEM Güvenlik Alarmları</li>
                  <li className="flex items-center gap-2"><CheckCircle className="w-3.5 h-3.5 text-indigo-400" /> 7/24 Teknik Destek</li>
                </ul>
              </div>
              <button
                onClick={() => router.push("/register")}
                className="w-full py-2.5 rounded-xl bg-gradient-to-r from-teal-500 to-indigo-500 hover:from-teal-400 hover:to-indigo-400 text-slate-950 font-black text-xs tracking-wider transition-all cursor-pointer"
              >
                Hemen Satın Al
              </button>
            </div>

            {/* Enterprise Plan */}
            <div className="p-6 rounded-2xl bg-slate-900/30 border border-slate-800/60 flex flex-col justify-between space-y-6">
              <div className="space-y-4">
                <div className="text-xs font-mono text-purple-400 font-bold uppercase">Kurumsal</div>
                <h3 className="text-xl font-bold text-slate-200">Enterprise</h3>
                <div className="text-3xl font-black text-slate-100 font-mono">Özel</div>
                <ul className="text-xs text-slate-400 space-y-2.5 pt-4 border-t border-slate-800/60 font-mono">
                  <li className="flex items-center gap-2"><CheckCircle className="w-3.5 h-3.5 text-purple-400" /> Dedicated AWS Altyapısı</li>
                  <li className="flex items-center gap-2"><CheckCircle className="w-3.5 h-3.5 text-purple-400" /> Dedicated Database Desteği</li>
                  <li className="flex items-center gap-2"><CheckCircle className="w-3.5 h-3.5 text-purple-400" /> SLA & SLA Güvenceleri</li>
                </ul>
              </div>
              <button
                onClick={() => alert("Kurumsal entegrasyonlar için lütfen 'sales@omnipulse.com' üzerinden iletişime geçin.")}
                className="w-full py-2.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-bold transition-all cursor-pointer"
              >
                Satışla İletişime Geç
              </button>
            </div>
          </div>
        </div>
      </section>

      {/* Footer */}
      <footer className="border-t border-slate-900 bg-slate-950 px-6 py-12 text-center text-xs font-mono text-slate-600 space-y-4">
        <div className="flex justify-center items-center gap-4">
          <a href="#features" className="hover:text-slate-400">Özellikler</a>
          <span>&bull;</span>
          <a href="#architecture" className="hover:text-slate-400">Mimari</a>
          <span>&bull;</span>
          <a href="#pricing" className="hover:text-slate-400">Fiyatlandırma</a>
        </div>
        <p>&copy; {new Date().getFullYear()} OmniPulse SaaS Platform. Tüm hakları saklıdır.</p>
        <p className="text-[10px] text-slate-700">Next.js 16 Secure BFF Pipeline &bull; Local HTTPS Integration</p>
      </footer>
    </div>
  );
}