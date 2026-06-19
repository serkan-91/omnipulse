import React from 'react';

export default function HomePage() {
  return (
    <main className="flex min-h-screen flex-col items-center justify-center bg-slate-950 text-white p-6 selection:bg-teal-500 selection:text-slate-950">
      {
        /* Üst Parıltı Efekti */
      }
      <div className="absolute top-0 left-1/2 -translate-x-1/2 w-[500px] h-[250px] bg-gradient-to-b from-teal-500/20 to-transparent blur-[120px] pointer-events-none" />

      <div className="z-10 text-center space-y-6 max-w-2xl">
        {
          /* Logo / Badge */
        }
        <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-teal-500/10 border border-teal-500/30 text-teal-400 text-xs font-mono uppercase tracking-wider animate-pulse">
          ⚡ OmniPulse Core System v1.0
        </div>

        {
          /* Başlık */
        }
        <h1 className="text-4xl md:text-6xl font-black tracking-tight bg-clip-text text-transparent bg-gradient-to-r from-white via-slate-200 to-slate-400">
          Next.js 16 Gateway
        </h1>

        {
          /* Mimarimizden Esintiler */
        }
        <p className="text-sm md:text-base text-slate-400 font-medium leading-relaxed max-w-lg mx-auto">
          Hollanda, Danimarka ve Polonya kurumsal standartlarına uygun,
          <span className="text-teal-400 font-mono"> Clean Architecture </span>
          ve çoklu veri tabanı (<span className="text-teal-400 font-mono">Multi-Tenant</span>) tabanlı .NET mikroservis ekosistemi ön yüzü.
        </p>

        {
          /* Altyapı Durum Kartları */
        }
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 pt-4 text-left">
          <div className="p-4 rounded-xl bg-slate-900/60 border border-slate-800/80 hover:border-teal-500/40 transition-colors backdrop-blur-sm">
            <div className="text-xs font-mono text-slate-500 uppercase">Identity Service</div>
            <div className="text-sm font-bold text-slate-300 mt-1 flex items-center gap-1.5">
              <span className="w-2 h-2 rounded-full bg-amber-500 animate-ping" />
              Connecting...
            </div>
          </div>

          <div className="p-4 rounded-xl bg-slate-900/60 border border-slate-800/80 hover:border-teal-500/40 transition-colors backdrop-blur-sm">
            <div className="text-xs font-mono text-slate-500 uppercase">UI Framework</div>
            <div className="text-sm font-bold text-teal-400 mt-1 font-mono">Next.js 16 (App)</div>
          </div>

          <div className="p-4 rounded-xl bg-slate-900/60 border border-slate-800/80 hover:border-teal-500/40 transition-colors backdrop-blur-sm">
            <div className="text-xs font-mono text-slate-500 uppercase">Compiler Engine</div>
            <div className="text-sm font-bold text-emerald-400 mt-1 font-mono">Turbopack ⚡</div>
          </div>
        </div>
      </div>
    </main>
  );
}