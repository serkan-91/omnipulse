"use client";

import React, { useState } from "react";
import { useRouter } from "next/navigation";
import {
  Zap,
  Building2,
  User,
  Mail,
  Lock,
  ArrowRight,
  RefreshCw,
  AlertTriangle,
  CheckCircle2
} from "lucide-react";

export default function RegisterTenant() {
  const router = useRouter();

  // Form states
  const [companyName, setCompanyName] = useState("");
  const [tenantIdentifier, setTenantIdentifier] = useState("");
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");

  // UI status states
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  // Normalize tenant identifier as slug (lowercase, alphanumeric + dashes)
  const handleTenantIdChange = (val: string) => {
    const normalized = val
      .toLowerCase()
      .replace(/[^a-z0-9-]/g, ""); // Keep only lowercase a-z, 0-9, and dashes
    setTenantIdentifier(normalized);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);
    setSuccess(null);

    if (!tenantIdentifier.trim()) {
      setError("Şirket kısa adı boş olamaz.");
      setLoading(false);
      return;
    }

    try {
      const response = await fetch("/api/bff/register", {
        method: "POST",
        headers: {
          "Content-Type": "application/json"
        },
        body: JSON.stringify({
          companyName: companyName.trim(),
          tenantIdentifier: tenantIdentifier.trim(),
          firstName: firstName.trim(),
          lastName: lastName.trim(),
          email: email.trim().toLowerCase(),
          password: password
        })
      });

      const data = await response.json();

      if (!response.ok || !data.isSuccess) {
        throw new Error(data.message || "Şirket kurulumu sırasında bir hata oluştu.");
      }

      setSuccess("Şirketiniz kuruldu ve yöneticiniz oluşturuldu! Giriş yapılıyor... 🚀");
      
      // Redirect to main page after 1.5 seconds (cookie is already set by BFF)
      setTimeout(() => {
        router.push("/");
        router.refresh();
      }, 1500);

    } catch (err: unknown) {
      const errMsg = err instanceof Error ? err.message : "Beklenmeyen bir hata oluştu.";
      setError(errMsg);
      setLoading(false);
    }
  };

  return (
    <main className="flex min-h-screen flex-col items-center justify-center bg-slate-950 text-slate-100 font-sans relative overflow-hidden px-4 py-8">
      {/* Background Glows */}
      <div className="absolute top-1/4 left-1/4 w-[500px] h-[500px] bg-teal-500/10 blur-[150px] pointer-events-none rounded-full" />
      <div className="absolute bottom-1/4 right-1/4 w-[500px] h-[500px] bg-indigo-500/10 blur-[150px] pointer-events-none rounded-full" />

      {/* Glassmorphic Container */}
      <div className="z-10 w-full max-w-lg p-8 rounded-3xl bg-slate-900/40 border border-slate-800/80 backdrop-blur-xl shadow-2xl space-y-6">
        
        {/* Title */}
        <div className="flex flex-col items-center space-y-2">
          <div className="p-3 rounded-2xl bg-gradient-to-tr from-teal-500 to-indigo-500 text-slate-950 shadow-lg cursor-pointer hover:scale-105 transition-transform duration-200" onClick={() => router.push("/")}>
            <Zap className="w-8 h-8" />
          </div>
          <h1 className="text-2xl font-black tracking-wider bg-clip-text text-transparent bg-gradient-to-r from-white via-slate-100 to-slate-400 text-center uppercase">
            YENİ ŞİRKET KURULUMU
          </h1>
          <p className="text-xs text-slate-400 font-mono text-center">
            OmniPulse SaaS Platformu &bull; Self-Service Tenant Admin Registration
          </p>
        </div>

        {/* Form */}
        <form onSubmit={handleSubmit} className="space-y-5">
          
          {/* Section: Şirket Bilgileri */}
          <div className="space-y-4">
            <div className="flex items-center gap-2 pb-1 border-b border-slate-800/60">
              <Building2 className="w-4 h-4 text-teal-400" />
              <span className="text-xs font-bold text-slate-400 uppercase tracking-wider">Şirket Detayları</span>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div>
                <label className="block text-[11px] font-bold text-slate-400 uppercase tracking-wide mb-1">Şirket Resmi Adı</label>
                <input
                  type="text"
                  required
                  value={companyName}
                  onChange={(e) => setCompanyName(e.target.value)}
                  placeholder="Örn: Panda Lojistik A.Ş."
                  className="w-full px-4 py-3 rounded-xl bg-slate-950/80 border border-slate-800/60 focus:border-teal-500 focus:ring-1 focus:ring-teal-500 text-sm outline-none transition-all"
                />
              </div>

              <div>
                <label className="block text-[11px] font-bold text-slate-400 uppercase tracking-wide mb-1">
                  Şirket Kısa Adı (ID / Slug)
                </label>
                <input
                  type="text"
                  required
                  value={tenantIdentifier}
                  onChange={(e) => handleTenantIdChange(e.target.value)}
                  placeholder="Örn: pandalojistik"
                  className="w-full px-4 py-3 rounded-xl bg-slate-950/80 border border-slate-800/60 focus:border-teal-500 focus:ring-1 focus:ring-teal-500 text-sm outline-none transition-all font-mono"
                />
                <span className="text-[10px] text-slate-500 mt-1 block">
                  URL ve sistem kimliğinde kullanılır: <strong>{tenantIdentifier || "slug"}</strong>
                </span>
              </div>
            </div>
          </div>

          {/* Section: Kurucu & Yönetici Bilgileri */}
          <div className="space-y-4 pt-2">
            <div className="flex items-center gap-2 pb-1 border-b border-slate-800/60">
              <User className="w-4 h-4 text-indigo-400" />
              <span className="text-xs font-bold text-slate-400 uppercase tracking-wider">Yönetici Profil Bilgileri</span>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div>
                <label className="block text-[11px] font-bold text-slate-400 uppercase tracking-wide mb-1">Adınız</label>
                <input
                  type="text"
                  required
                  value={firstName}
                  onChange={(e) => setFirstName(e.target.value)}
                  placeholder="Ahmet"
                  className="w-full px-4 py-3 rounded-xl bg-slate-950/80 border border-slate-800/60 focus:border-teal-500 focus:ring-1 focus:ring-teal-500 text-sm outline-none transition-all"
                />
              </div>

              <div>
                <label className="block text-[11px] font-bold text-slate-400 uppercase tracking-wide mb-1">Soyadınız</label>
                <input
                  type="text"
                  required
                  value={lastName}
                  onChange={(e) => setLastName(e.target.value)}
                  placeholder="Yılmaz"
                  className="w-full px-4 py-3 rounded-xl bg-slate-950/80 border border-slate-800/60 focus:border-teal-500 focus:ring-1 focus:ring-teal-500 text-sm outline-none transition-all"
                />
              </div>
            </div>

            <div>
              <label className="block text-[11px] font-bold text-slate-400 uppercase tracking-wide mb-1">E-Posta Adresi</label>
              <div className="relative">
                <input
                  type="email"
                  required
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder="admin@pandalojistik.com"
                  className="w-full pl-10 pr-4 py-3 rounded-xl bg-slate-950/80 border border-slate-800/60 focus:border-teal-500 focus:ring-1 focus:ring-teal-500 text-sm outline-none transition-all"
                />
                <Mail className="absolute left-3.5 top-3.5 w-4 h-4 text-slate-500" />
              </div>
            </div>

            <div>
              <label className="block text-[11px] font-bold text-slate-400 uppercase tracking-wide mb-1">Şifre</label>
              <div className="relative">
                <input
                  type="password"
                  required
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder="••••••••"
                  className="w-full pl-10 pr-4 py-3 rounded-xl bg-slate-950/80 border border-slate-800/60 focus:border-teal-500 focus:ring-1 focus:ring-teal-500 text-sm outline-none transition-all"
                />
                <Lock className="absolute left-3.5 top-3.5 w-4 h-4 text-slate-500" />
              </div>
            </div>
          </div>

          {/* Feedback Messages */}
          {error && (
            <div className="p-3.5 rounded-xl bg-rose-500/10 border border-rose-500/30 text-rose-400 text-xs font-mono flex items-start gap-2.5">
              <AlertTriangle className="w-4 h-4 shrink-0 text-rose-500" />
              <span>{error}</span>
            </div>
          )}

          {success && (
            <div className="p-3.5 rounded-xl bg-emerald-500/10 border border-emerald-500/30 text-emerald-400 text-xs font-mono flex items-start gap-2.5">
              <CheckCircle2 className="w-4 h-4 shrink-0 text-emerald-400 animate-bounce" />
              <span>{success}</span>
            </div>
          )}

          {/* Submit & Back buttons */}
          <div className="pt-2 flex flex-col sm:flex-row gap-3">
            <button
              type="button"
              onClick={() => router.push("/")}
              className="w-full sm:w-1/3 py-3 rounded-xl border border-slate-800 bg-slate-900/60 hover:bg-slate-800 text-slate-300 text-sm font-bold transition-all active:scale-95 text-center cursor-pointer"
            >
              Geri Dön
            </button>
            <button
              type="submit"
              disabled={loading || !!success}
              className="w-full sm:w-2/3 py-3 rounded-xl bg-gradient-to-r from-teal-500 to-indigo-500 text-slate-950 hover:from-teal-400 hover:to-indigo-400 font-bold text-sm tracking-wide transition-all duration-200 hover:shadow-lg active:scale-98 disabled:opacity-50 flex items-center justify-center gap-2 cursor-pointer"
            >
              {loading ? (
                <>
                  <RefreshCw className="w-4 h-4 animate-spin" />
                  Şirket Kuruluyor...
                </>
              ) : (
                <>
                  Kurulumu Başlat
                  <ArrowRight className="w-4 h-4" />
                </>
              )}
            </button>
          </div>
        </form>

        {/* Footer info */}
        <div className="pt-4 border-t border-slate-900 text-center">
          <span className="text-[10px] text-slate-500 font-mono">
            Kurucu hesabı otomatik olarak &quot;Owner&quot; rolüyle yetkilendirilir.
          </span>
        </div>
      </div>
    </main>
  );
}
