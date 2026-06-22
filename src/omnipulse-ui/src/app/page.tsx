"use client";

import React, { useState, useEffect, useRef } from "react";
import * as signalR from "@microsoft/signalr";
import {
  Activity,
  Cpu,
  Database,
  Play,
  Pause,
  Send,
  Terminal,
  Wifi,
  WifiOff,
  Zap,
  Flame,
  Droplet,
  Settings,
  RefreshCw,
  AlertTriangle,
  Power,
  ShieldAlert,
  UserPlus,
  ShieldCheck
} from "lucide-react";

interface TelemetryLog {
  id: string;
  deviceId: string;
  key: string;
  value: number;
  timestamp: string;
  raw: unknown;
}

interface SecurityAlert {
  id: string;
  action: string;
  deviceSerialNumber: string;
  message: string;
  timestamp: string;
}

interface DeviceState {
  id: string;
  name: string;
  serialNumber: string;
  tenantId: string;
  lastTemp: number;
  lastPress: number;
  isOnline: boolean;
  history: { temp: number; press: number; time: string }[];
}

interface TelemetryPayload {
  deviceId?: string;
  telemetryKey?: string;
  telemetryValue?: number;
  timestamp?: string;
}

interface SecurityAlertPayload {
  action?: string;
  deviceSerialNumber?: string;
  message?: string;
}

interface DeviceStatusPayload {
  deviceSerialNumber?: string;
  isOnline?: boolean;
}

const SEED_DEVICES = [
  { serialNumber: "SN-AUPANDA-TEMP", name: "AUPanda01 Sıcaklık Sensörü", tenantId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", unit: "°C" },
  { serialNumber: "SN-BTA1-WATER", name: "Bant A1 Su Seviye Sensörü", tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", unit: "L" },
  { serialNumber: "SN-BTA1-VIB", name: "Bant A1 Titreşim Sensörü", tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", unit: "Hz" },
  { serialNumber: "SN-BTB1-OIL", name: "Bant B1 Yağ Sensörü", tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", unit: "%" }
];

const MOCK_DEVICES: Record<string, DeviceState> = {
  "SN-AUPANDA-TEMP": { id: "SN-AUPANDA-TEMP", name: "AUPanda01 (Tır)", serialNumber: "SN-AUPANDA-TEMP", tenantId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", lastTemp: 25.0, lastPress: 1000.0, isOnline: true, history: [] },
  "SN-BTA1-WATER": { id: "SN-BTA1-WATER", name: "Bant A1 (Su Deposu)", serialNumber: "SN-BTA1-WATER", tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", lastTemp: 25.0, lastPress: 1000.0, isOnline: true, history: [] },
  "SN-BTA1-VIB": { id: "SN-BTA1-VIB", name: "Bant A1 (Titreşim)", serialNumber: "SN-BTA1-VIB", tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", lastTemp: 25.0, lastPress: 1000.0, isOnline: true, history: [] },
  "SN-BTB1-OIL": { id: "SN-BTB1-OIL", name: "Bant B1 (Yağ)", serialNumber: "SN-BTB1-OIL", tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", lastTemp: 25.0, lastPress: 1000.0, isOnline: true, history: [] }
};

export default function TelemetryDashboard() {
  const showSimulators = process.env.NODE_ENV !== "production";

  // Connection and API states
  const [hubStatus, setHubStatus] = useState<"Disconnected" | "Connecting" | "Connected" | "Reconnecting" | "Error">("Disconnected");
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [seedLogs, setSeedLogs] = useState<string | null>(null);
  const [isSeeding, setIsSeeding] = useState(false);

  // Simulator states
  const [selectedDevice, setSelectedDevice] = useState(SEED_DEVICES[0].serialNumber);
  const [simTemp, setSimTemp] = useState<number>(25.0);
  const [simPress, setSimPress] = useState<number>(1000.0);
  const [isAutoSim, setIsAutoSim] = useState(false);
  const [isSending, setIsSending] = useState(false);

  // Dashboard Telemetry states
  const [devices, setDevices] = useState<Record<string, DeviceState>>({});

  const [logs, setLogs] = useState<TelemetryLog[]>([]);
  const [alerts, setAlerts] = useState<SecurityAlert[]>([]);
  const terminalEndRef = useRef<HTMLDivElement>(null);
  const connectionRef = useRef<signalR.HubConnection | null>(null);

  // BFF Authentication States 🔒
  const [user, setUser] = useState<{ email: string; tenantIdentifier: string; roles: string[] } | null>(null);
  const [isGuestMode, setIsGuestMode] = useState(false);
  const [authChecked, setAuthChecked] = useState(false);
  const [loginEmail, setLoginEmail] = useState("");
  const [loginPassword, setLoginPassword] = useState("");
  const [loginTenant, setLoginTenant] = useState("");
  const [loginError, setLoginError] = useState<string | null>(null);
  const [isLoggingIn, setIsLoggingIn] = useState(false);

  // Tenant Status & Invite states 👥
  const [tenantStatus, setTenantStatus] = useState<{ message: string; detectedTenant: string; tenantName: string; activeConnectionString: string } | null>(null);
  const [inviteEmail, setInviteEmail] = useState("");
  const [inviteRole, setInviteRole] = useState("Member");
  const [inviteLoading, setInviteLoading] = useState(false);
  const [inviteError, setInviteError] = useState<string | null>(null);
  const [inviteSuccess, setInviteSuccess] = useState<string | null>(null);

  // IoT Demo & Devices States
  const [isDemoSeeding, setIsDemoSeeding] = useState(false);
  const [isDemoCleaning, setIsDemoCleaning] = useState(false);
  const [isUserDevicesLoaded, setIsUserDevicesLoaded] = useState(false);
  const [userDevicesList, setUserDevicesList] = useState<{ serialNumber: string; name: string; unit: string }[]>([]);

  // Auto-scroll terminal log
  useEffect(() => {
    terminalEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [logs, alerts]);

  // Check active BFF session on mount
  useEffect(() => {
    const checkAuth = async () => {
      try {
        const res = await fetch("/api/bff/user");
        if (res.ok) {
          const data = await res.json();
          if (data.isAuthenticated) {
            const rolesClaim = data.roles || [];
            const normalizedRoles = Array.isArray(rolesClaim) ? rolesClaim : [rolesClaim];
            setUser({
              email: data.email,
              tenantIdentifier: data.tenantIdentifier,
              roles: normalizedRoles
            });
          } else {
            setDevices(MOCK_DEVICES);
          }
        } else {
          setDevices(MOCK_DEVICES);
        }
      } catch (err) {
        console.error("BFF auth check failed:", err);
        setDevices(MOCK_DEVICES);
      } finally {
        setAuthChecked(true);
      }
    };
    checkAuth();
  }, []);

  // Fetch tenant status when user changes
  useEffect(() => {
    const fetchTenantStatus = async () => {
      if (!user) {
        setTenantStatus(null);
        return;
      }
      try {
        const res = await fetch("/api/bff/proxy/api/tenants/current-status");
        if (res.ok) {
          const data = await res.json();
          setTenantStatus(data);
        }
      } catch (err) {
        console.error("Failed to fetch tenant status:", err);
      }
    };
    fetchTenantStatus();
  }, [user]);

  const fetchUserDevices = async () => {
    if (!user) {
      setUserDevicesList([]);
      setIsUserDevicesLoaded(false);
      return;
    }
    try {
      const res = await fetch("/api/bff/proxy/api/iot/devices");
      if (res.ok) {
        const data = await res.json();
        const deviceList = data.map((d: any) => {
          let unit = "°C";
          if (d.name.toLowerCase().includes("vibr") || d.serialNumber.toLowerCase().includes("vib")) unit = "Hz";
          else if (d.name.toLowerCase().includes("press") || d.serialNumber.toLowerCase().includes("pres")) unit = "kPa";
          return {
            serialNumber: d.serialNumber,
            name: d.name + (d.assetName ? ` (${d.assetName})` : ""),
            unit: unit
          };
        });
        setUserDevicesList(deviceList);
        
        setDevices(prev => {
          const newDevices: Record<string, DeviceState> = {};
          data.forEach((d: any) => {
            const existing = prev[d.serialNumber] || {
              lastTemp: 25.0,
              lastPress: 1000.0,
              isOnline: d.isActive,
              history: []
            };
            newDevices[d.serialNumber] = {
              id: d.id,
              name: d.name + (d.assetName ? ` (${d.assetName})` : ""),
              serialNumber: d.serialNumber,
              tenantId: "",
              lastTemp: existing.lastTemp,
              lastPress: existing.lastPress,
              isOnline: d.isActive,
              history: existing.history
            };
          });
          return newDevices;
        });
      }
    } catch (err) {
      console.error("Failed to fetch tenant devices:", err);
    } finally {
      setIsUserDevicesLoaded(true);
    }
  };

  // Fetch real tenant devices on user login
  useEffect(() => {
    fetchUserDevices();
  }, [user]);

  // Synchronize selectedDevice when device list updates
  useEffect(() => {
    const list = user ? userDevicesList : SEED_DEVICES;
    if (list.length > 0) {
      setSelectedDevice(list[0].serialNumber);
    }
  }, [user, userDevicesList]);

  // Login handler submitting to Next.js BFF
  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoggingIn(true);
    setLoginError(null);
    try {
      const res = await fetch("/api/bff/login", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          email: loginEmail,
          password: loginPassword,
          tenantIdentifier: loginTenant || undefined
        })
      });

      const data = await res.json();
      if (!res.ok || !data.isSuccess) {
        throw new Error(data.message || "E-posta veya şifre hatalı.");
      }

      // Load user profile
      const userRes = await fetch("/api/bff/user");
      if (userRes.ok) {
        const userData = await userRes.json();
        const rolesClaim = userData.roles || [];
        const normalizedRoles = Array.isArray(rolesClaim) ? rolesClaim : [rolesClaim];
        setUser({
          email: userData.email,
          tenantIdentifier: userData.tenantIdentifier,
          roles: normalizedRoles
        });
      }
    } catch (err: unknown) {
      const errMsg = err instanceof Error ? err.message : "Giriş hatası oluştu.";
      setLoginError(errMsg);
    } finally {
      setIsLoggingIn(false);
    }
  };

  // Invite user command handler
  const handleSendInvite = async (e: React.FormEvent) => {
    e.preventDefault();
    setInviteLoading(true);
    setInviteError(null);
    setInviteSuccess(null);

    try {
      const res = await fetch("/api/bff/proxy/api/tenants/invite", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          email: inviteEmail.trim().toLowerCase(),
          role: inviteRole
        })
      });

      const data = await res.json();
      if (!res.ok || !data.isSuccess) {
        throw new Error(data.message || "Davet gönderilemedi.");
      }

      setInviteSuccess(data.message || "Kullanıcı başarıyla davet edildi.");
      setInviteEmail("");
    } catch (err: unknown) {
      const errMsg = err instanceof Error ? err.message : "Davet işlemi başarısız oldu.";
      setInviteError(errMsg);
    } finally {
      setInviteLoading(false);
    }
  };

  // Logout handler clearing BFF cookies
  const handleLogout = async () => {
    try {
      await fetch("/api/bff/logout", { method: "POST" });
    } catch (err) {
      console.error("Logout failed:", err);
    } finally {
      setUser(null);
      if (connectionRef.current) {
        await connectionRef.current.stop();
      }
    }
  };

  // Connect to SignalR with temporary WS access token from BFF
  useEffect(() => {
    let activeConnection: signalR.HubConnection | null = null;

    const connectToHub = async () => {
      if (!authChecked || !user) {
        setHubStatus("Disconnected");
        return;
      }

      const hubUrl = "https://localhost:7122/hubs/telemetry";

      console.log(`Connecting to SignalR Hub at: ${hubUrl}`);
      setHubStatus("Connecting");

      const newConnection = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl, {
          accessTokenFactory: async () => {
            try {
              const res = await fetch("/api/bff/ws-token");
              if (res.ok) {
                const data = await res.json();
                return data.token || "";
              }
            } catch (err) {
              console.error("WS Token fetch failed:", err);
            }
            return "";
          }
        })
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: (retryContext) => {
            // Exponential backoff: 2s, 4s, 8s, 16s, max 30s
            const delay = Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
            console.log(`[SignalR Reconnect] Attempt #${retryContext.previousRetryCount + 1}. Delaying for ${delay}ms.`);
            return delay;
          }
        })
        .build();

      // 1. Telemetry Data Listener
      newConnection.on("ReceiveTelemetry", (data: TelemetryPayload) => {
        console.log("Telemetry received via SignalR:", data);
        
        const parsedLog: TelemetryLog = {
          id: Math.random().toString(36).substr(2, 9),
          deviceId: data.deviceId || "Bilinmiyor",
          key: data.telemetryKey || "Metric",
          value: Number(data.telemetryValue?.toFixed(2)) || 0,
          timestamp: new Date(data.timestamp || Date.now()).toLocaleTimeString(),
          raw: data
        };

        setLogs((prev) => [...prev.slice(-49), parsedLog]);

        setDevices((prev) => {
          const devKey = Object.keys(prev).find(
            (k) => k === data.deviceId || (data.deviceId && prev[k].name.includes(data.deviceId)) || prev[k].serialNumber === data.deviceId
          ) || data.deviceId || "";

          const currentDevice = prev[devKey] || {
            id: data.deviceId || "",
            name: data.deviceId || "",
            serialNumber: data.deviceId || "",
            tenantId: "",
            lastTemp: 0,
            lastPress: 0,
            isOnline: true,
            history: []
          };

          const isTemp = data.telemetryKey === "temperature";
          const val = Number(data.telemetryValue) || 0;

          const updatedHistory = [
            ...currentDevice.history,
            {
              temp: isTemp ? val : currentDevice.lastTemp,
              press: !isTemp ? val : currentDevice.lastPress,
              time: new Date().toLocaleTimeString().substr(3, 5)
            }
          ].slice(-15);

          return {
            ...prev,
            [devKey]: {
              ...currentDevice,
              lastTemp: isTemp ? val : currentDevice.lastTemp,
              lastPress: !isTemp ? val : currentDevice.lastPress,
              history: updatedHistory
            }
          };
        });
      });

      // 2. Security Alerts Listener (SIEM integration) 🚨
      newConnection.on("ReceiveSecurityAlert", (data: SecurityAlertPayload) => {
        console.warn("⚠️ Security Alert Received via SignalR:", data);

        const parsedAlert: SecurityAlert = {
          id: Math.random().toString(36).substr(2, 9),
          action: data.action || "Warning",
          deviceSerialNumber: data.deviceSerialNumber || "UNKNOWN",
          message: data.message || "Güvenlik İhlal Girişimi!",
          timestamp: new Date().toLocaleTimeString()
        };

        setAlerts((prev) => [...prev.slice(-9), parsedAlert]);
      });

      // 3. Device Connection Status Listener 🔌
      newConnection.on("ReceiveDeviceStatus", (data: DeviceStatusPayload) => {
        console.log("Device connection status updated:", data);

        setDevices((prev) => {
          const devKey = Object.keys(prev).find(
            (k) => k === data.deviceSerialNumber
          ) || data.deviceSerialNumber || "";

          if (prev[devKey]) {
            return {
              ...prev,
              [devKey]: {
                ...prev[devKey],
                isOnline: !!data.isOnline
              }
            };
          }
          return prev;
        });
      });

      // 4. Connection State Events for Auto-Reconnect
      newConnection.onreconnecting((err) => {
        console.warn("SignalR connection lost. Reconnecting...", err);
        setHubStatus("Reconnecting");
      });

      newConnection.onreconnected((connectionId) => {
        console.log("SignalR reconnected successfully!", connectionId);
        setHubStatus("Connected");
        setErrorMessage(null);
      });

      newConnection.onclose((err) => {
        console.error("SignalR connection closed.", err);
        setHubStatus("Disconnected");
        if (err) {
          setErrorMessage("Bağlantı kapandı. Lütfen sayfayı yenileyin.");
        }
      });

      newConnection
        .start()
        .then(() => {
          console.log("SignalR Connected Successfully!");
          setHubStatus("Connected");
          setErrorMessage(null);
        })
        .catch((err) => {
          console.error("SignalR Connection Failed:", err);
          setHubStatus("Error");
          setErrorMessage(`Bağlantı Başarısız: API'nin çalışıp çalışmadığını (https://localhost:7122) kontrol edin.`);
        });

      activeConnection = newConnection;
      connectionRef.current = newConnection;
    };

    connectToHub();

    return () => {
      if (activeConnection) {
        activeConnection.stop();
      }
    };
  }, [authChecked, user]);

  // Send Telemetry HTTP Post
  const handleSendTelemetry = async (devId: string, tempVal: number, pressVal: number) => {
    setIsSending(true);
    try {
      const response = await fetch("/api/bff/proxy/api/iot/telemetry", {
        method: "POST",
        headers: {
          "Content-Type": "application/json"
        },
        body: JSON.stringify({
          deviceId: devId,
          temperature: tempVal,
          pressure: pressVal,
          timestamp: new Date().toISOString()
        })
      });

      if (!response.ok) {
        throw new Error(`Ingest HTTP error! status: ${response.status}`);
      }
    } catch (err: unknown) {
      console.error("Failed to ingest telemetry:", err);
      if (hubStatus !== "Connected") {
        simulateDirectTelemetry(devId, tempVal, pressVal);
      }
    } finally {
      setIsSending(false);
    }
  };

  // Send Connection Status Change POST
  const handleSendConnectionStatus = async (devId: string, isOnline: boolean) => {
    try {
      const response = await fetch("/api/bff/proxy/api/iot/devices/status", {
        method: "POST",
        headers: {
          "Content-Type": "application/json"
        },
        body: JSON.stringify({
          deviceId: devId,
          isOnline: isOnline,
          timestamp: new Date().toISOString()
        })
      });

      if (!response.ok) {
        throw new Error(`Status HTTP error! status: ${response.status}`);
      }
    } catch (err: unknown) {
      console.error("Failed to send status update:", err);
      // Fallback
      if (hubStatus !== "Connected") {
        setDevices((prev) => {
          if (prev[devId]) {
            return {
              ...prev,
              [devId]: {
                ...prev[devId],
                isOnline: isOnline
              }
            };
          }
          return prev;
        });
      }
    }
  };

  // Seeding endpoint trigger
  const handleSeedDemo = async () => {
    setIsSeeding(true);
    setSeedLogs("Demo veritabanı kuruluyor ve tablolar tohumlanıyor (seeding)... ⏳");
    try {
      const response = await fetch("/api/bff/proxy/api/workflows/demo", {
        method: "POST"
      });

      if (!response.ok) {
        throw new Error(`Seed HTTP error! status: ${response.status}`);
      }

      const result = await response.json();
      setSeedLogs(JSON.stringify(result, null, 2));
    } catch (err: unknown) {
      const errMsg = err instanceof Error ? err.message : String(err);
      setSeedLogs(`Seeding Hatası: ${errMsg}. Backend API'nin 7122 (HTTPS) portunda çalıştığından emin olun.`);
    } finally {
      setIsSeeding(false);
    }
  };

  const handleSeedIoTDemo = async () => {
    setIsDemoSeeding(true);
    setSeedLogs("Örnek IoT sensörleri, varlık yapılandırmaları ve geçmiş telemetri verileri oluşturuluyor... ⏳");
    try {
      const response = await fetch("/api/bff/proxy/api/iot/devices/seed-demo", {
        method: "POST"
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({ message: `HTTP error! status: ${response.status}` }));
        throw new Error(errorData.message || `HTTP error! status: ${response.status}`);
      }

      const result = await response.json();
      setSeedLogs(result.message || JSON.stringify(result, null, 2));
      await fetchUserDevices();
    } catch (err: unknown) {
      const errMsg = err instanceof Error ? err.message : String(err);
      setSeedLogs(`Demo Kurulum Hatası: ${errMsg}`);
    } finally {
      setIsDemoSeeding(false);
    }
  };

  const handleCleanupIoTDemo = async () => {
    if (!confirm("Tüm demo sensörleri, varlıkları ve telemetri geçmişini kalıcı olarak silmek istediğinizden emin misiniz?")) {
      return;
    }
    setIsDemoCleaning(true);
    setSeedLogs("Demo verileri temizleniyor ve envanter sıfırlanıyor... 🧹");
    try {
      const response = await fetch("/api/bff/proxy/api/iot/devices/cleanup-demo", {
        method: "POST"
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({ message: `HTTP error! status: ${response.status}` }));
        throw new Error(errorData.message || `HTTP error! status: ${response.status}`);
      }

      const result = await response.json();
      setSeedLogs(result.message || JSON.stringify(result, null, 2));
      setDevices({});
      setUserDevicesList([]);
    } catch (err: unknown) {
      const errMsg = err instanceof Error ? err.message : String(err);
      setSeedLogs(`Temizlik Hatası: ${errMsg}`);
    } finally {
      setIsDemoCleaning(false);
    }
  };

  // Direct mock simulation loop (fallback if backend localstack/kinesis is disconnected)
  const simulateDirectTelemetry = (devId: string, tempVal: number, pressVal: number) => {
    const mockEvents = [
      { key: "temperature", val: tempVal },
      { key: "pressure", val: pressVal }
    ];

    mockEvents.forEach((evt) => {
      const parsedLog: TelemetryLog = {
        id: Math.random().toString(36).substr(2, 9),
        deviceId: devId,
        key: evt.key,
        value: Number(evt.val.toFixed(2)),
        timestamp: new Date().toLocaleTimeString(),
        raw: { simulator: "local_fallback", deviceId: devId, key: evt.key, value: evt.val }
      };

      setLogs((prev) => [...prev.slice(-49), parsedLog]);

      setDevices((prev) => {
        const currentDevice = prev[devId];
        const isTemp = evt.key === "temperature";
        const val = Number(evt.val);

        const updatedHistory = [
          ...currentDevice.history,
          {
            temp: isTemp ? val : currentDevice.lastTemp,
            press: !isTemp ? val : currentDevice.lastPress,
            time: new Date().toLocaleTimeString().substr(3, 5)
          }
        ].slice(-15);

        return {
          ...prev,
          [devId]: {
            ...currentDevice,
            lastTemp: isTemp ? val : currentDevice.lastTemp,
            lastPress: !isTemp ? val : currentDevice.lastPress,
            history: updatedHistory
          }
        };
      });
    });
  };

  const handleSendTelemetryRef = useRef(handleSendTelemetry);
  useEffect(() => {
    handleSendTelemetryRef.current = handleSendTelemetry;
  });

  // Auto-simulation interval loop
  useEffect(() => {
    if (!isAutoSim) return;

    const interval = setInterval(() => {
      const activeList = user ? userDevicesList : SEED_DEVICES;
      activeList.forEach((dev) => {
        // Skip sending if device is offline (to show realistic connection flow)
        if (devices[dev.serialNumber]?.isOnline === false) return;

        let randTemp = 20 + Math.random() * 60; // 20 - 80
        const randPress = 970 + Math.random() * 70; // 970 - 1040

        if (dev.serialNumber.toLowerCase().includes("water")) {
          randTemp = 10 + Math.random() * 25;
        } else if (dev.serialNumber.toLowerCase().includes("vib")) {
          randTemp = 50 + Math.random() * 110;
        } else if (dev.serialNumber.toLowerCase().includes("oil")) {
          randTemp = 15 + Math.random() * 50;
        } else if (dev.serialNumber.toLowerCase().includes("temp")) {
          randTemp = 4.0 + Math.random() * 1.5;
        } else if (dev.serialNumber.toLowerCase().includes("pres")) {
          randTemp = 24.5 + Math.random() * 1.0;
        }

        handleSendTelemetryRef.current(dev.serialNumber, randTemp, randPress);
      });
    }, 2500);

    return () => clearInterval(interval);
  }, [isAutoSim, devices, user, userDevicesList]);

  // Generate SVG path for histories
  const generateSvgPath = (history: { temp: number; press: number }[], type: "temp" | "press", width: number, height: number) => {
    if (history.length < 2) return "";
    const values = history.map((h) => (type === "temp" ? h.temp : h.press));
    const minVal = Math.min(...values) - 2;
    const maxVal = Math.max(...values) + 2;
    const valRange = maxVal - minVal || 1;

    const points = history.map((h, i) => {
      const val = type === "temp" ? h.temp : h.press;
      const x = (i / (history.length - 1)) * width;
      const y = height - ((val - minVal) / valRange) * height;
      return `${x},${y}`;
    });

    return `M ${points.join(" L ")}`;
  };

  // Loading state during auth check
  if (!authChecked) {
    return (
      <main className="flex min-h-screen items-center justify-center bg-slate-950">
        <div className="flex flex-col items-center space-y-4">
          <RefreshCw className="w-8 h-8 text-teal-400 animate-spin" />
          <span className="text-xs text-slate-400 font-mono">Güvenli BFF oturumu doğrulanıyor... 🛡️</span>
        </div>
      </main>
    );
  }

  // Login form overlay if not logged in and not in guest mode
  if (!user && !isGuestMode) {
    return (
      <main className="flex min-h-screen flex-col items-center justify-center bg-slate-950 text-slate-100 font-sans relative overflow-hidden">
        {/* Decorative Glows */}
        <div className="absolute top-1/4 left-1/4 w-[400px] h-[400px] bg-teal-500/10 blur-[150px] pointer-events-none rounded-full" />
        <div className="absolute bottom-1/4 right-1/4 w-[400px] h-[400px] bg-indigo-500/10 blur-[150px] pointer-events-none rounded-full" />

        {/* Glassmorphic Card */}
        <div className="z-10 w-full max-w-md p-8 rounded-3xl bg-slate-900/40 border border-slate-800/80 backdrop-blur-xl shadow-2xl space-y-6">
          <div className="flex flex-col items-center space-y-2">
            <div className="p-3 rounded-2xl bg-gradient-to-tr from-teal-500 to-indigo-500 text-slate-950 shadow-lg">
              <Zap className="w-8 h-8 animate-pulse" />
            </div>
            <h1 className="text-2xl font-black tracking-wider bg-clip-text text-transparent bg-gradient-to-r from-white via-slate-100 to-slate-400">
              OMNIPULSE BFF PORTAL
            </h1>
            <p className="text-xs text-slate-400 font-mono text-center">
              Next.js 16 BFF (Backend For Frontend) &bull; Secure Session
            </p>
          </div>

          <form onSubmit={handleLogin} className="space-y-4">
            <div>
              <label className="block text-xs font-bold text-slate-400 uppercase tracking-wide mb-1">E-Posta</label>
              <input
                type="email"
                required
                value={loginEmail}
                onChange={(e) => setLoginEmail(e.target.value)}
                placeholder="ornek@omnipulse.com"
                className="w-full px-4 py-3 rounded-xl bg-slate-950/80 border border-slate-800/60 focus:border-teal-500 focus:ring-1 focus:ring-teal-500 text-sm outline-none transition-all"
              />
            </div>

            <div>
              <label className="block text-xs font-bold text-slate-400 uppercase tracking-wide mb-1">Şifre</label>
              <input
                type="password"
                required
                value={loginPassword}
                onChange={(e) => setLoginPassword(e.target.value)}
                placeholder="••••••••"
                className="w-full px-4 py-3 rounded-xl bg-slate-950/80 border border-slate-800/60 focus:border-teal-500 focus:ring-1 focus:ring-teal-500 text-sm outline-none transition-all"
              />
            </div>

            <div>
              <label className="block text-xs font-bold text-slate-400 uppercase tracking-wide mb-1">Şirket Kodu (Tenant - Opsiyonel)</label>
              <input
                type="text"
                value={loginTenant}
                onChange={(e) => setLoginTenant(e.target.value)}
                placeholder="Örn: pandaberry"
                className="w-full px-4 py-3 rounded-xl bg-slate-950/80 border border-slate-800/60 focus:border-teal-500 focus:ring-1 focus:ring-teal-500 text-sm outline-none transition-all"
              />
            </div>

            {loginError && (
              <div className="p-3.5 rounded-xl bg-rose-500/10 border border-rose-500/30 text-rose-400 text-xs font-mono">
                {loginError}
              </div>
            )}

            <button
              type="submit"
              disabled={isLoggingIn}
              className="w-full py-3.5 rounded-xl bg-gradient-to-r from-teal-500 to-indigo-500 text-slate-950 hover:from-teal-400 hover:to-indigo-400 font-bold text-sm tracking-wide transition-all duration-200 hover:shadow-lg active:scale-98 disabled:opacity-50"
            >
              {isLoggingIn ? "Bağlanıyor..." : "Giriş Yap"}
            </button>

            <button
              type="button"
              onClick={() => setIsGuestMode(true)}
              className="w-full py-3.5 rounded-xl border border-slate-800 bg-slate-950 hover:bg-slate-900 text-teal-400 font-bold text-sm tracking-wide transition-all duration-200 active:scale-98 hover:shadow-lg hover:shadow-teal-500/5 cursor-pointer"
            >
              Canlı Demoyu İncele (Girişsiz) ⚡
            </button>
          </form>

          <div className="pt-4 border-t border-slate-900 text-center flex flex-col gap-2.5">
            <button
              type="button"
              onClick={() => window.location.href = "/register"}
              className="text-xs text-teal-400 hover:text-teal-300 font-bold underline transition-colors cursor-pointer"
            >
              Şirket Kurucusu musunuz? Yeni Şirket Kaydı &rarr;
            </button>
            <span className="text-[10px] text-slate-500 font-mono">
              BFF Mode: Token&apos;lar sunucu tarafında HttpOnly çerezlerde kilitlidir.
            </span>
          </div>
        </div>
      </main>
    );
  }

  return (
    <main className="flex min-h-screen flex-col bg-slate-950 text-slate-100 font-sans selection:bg-teal-500 selection:text-slate-950 relative overflow-hidden">
      {/* Decorative Glows */}
      <div className="absolute top-0 right-0 w-[600px] h-[300px] bg-gradient-to-b from-teal-500/10 to-indigo-500/5 blur-[120px] pointer-events-none" />
      <div className="absolute bottom-0 left-0 w-[600px] h-[300px] bg-gradient-to-t from-indigo-500/10 to-teal-500/5 blur-[120px] pointer-events-none" />

      {/* Header */}
      <header className="z-10 border-b border-slate-900 bg-slate-950/80 backdrop-blur-md sticky top-0 px-6 py-4 flex flex-col sm:flex-row items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <div className="p-2 rounded-lg bg-gradient-to-tr from-teal-500 to-indigo-500 text-slate-950">
            <Zap className="w-6 h-6 animate-pulse" />
          </div>
          <div>
            <h1 className="text-xl font-black tracking-wider bg-clip-text text-transparent bg-gradient-to-r from-white via-slate-100 to-slate-400">
              OMNIPULSE SECURE TELEMETRY
            </h1>
            <p className="text-xs text-slate-500 font-mono">Real-Time Kinesis SIEM & WebSocket Streaming Pipeline</p>
          </div>
        </div>

        {/* User Info & Connection Indicators */}
        <div className="flex items-center gap-4">
          <div className="hidden md:flex flex-col items-end text-xs font-mono">
            <span className="text-slate-200 font-bold">{user ? user.email : "Demo Ziyaretçi (Guest)"}</span>
            <span className="text-slate-500 text-[10px]">
              {user ? (user.tenantIdentifier ? `@${user.tenantIdentifier}` : "") : "@demo-sandbox"} &bull; {user ? user.roles.join(", ") : "Ziyaretçi"}
            </span>
          </div>

          <div className={`flex items-center gap-2 px-3 py-1.5 rounded-full border text-xs font-mono transition-all ${
            hubStatus === "Connected"
              ? "bg-emerald-500/10 border-emerald-500/30 text-emerald-400"
              : hubStatus === "Connecting"
              ? "bg-amber-500/10 border-amber-500/30 text-amber-400"
              : "bg-rose-500/10 border-rose-500/30 text-rose-400"
          }`}>
            <span className={`w-2.5 h-2.5 rounded-full ${
              hubStatus === "Connected"
                ? "bg-emerald-500 animate-ping"
                : hubStatus === "Connecting"
                ? "bg-amber-500 animate-pulse"
                : "bg-rose-500"
            }`} />
            WS Gateway: {hubStatus}
          </div>

          {showSimulators && (
            user ? (
              userDevicesList.length > 0 ? (
                <button
                  onClick={handleCleanupIoTDemo}
                  disabled={isDemoCleaning}
                  className="flex items-center gap-2 px-4 py-1.5 rounded-lg bg-rose-600 hover:bg-rose-500 text-white text-xs font-bold transition-all disabled:opacity-50 active:scale-95 cursor-pointer"
                >
                  <Database className="w-3.5 h-3.5" />
                  {isDemoCleaning ? "Temizleniyor..." : "Demo Verileri Temizle (Reset)"}
                </button>
              ) : (
                <button
                  onClick={handleSeedIoTDemo}
                  disabled={isDemoSeeding}
                  className="flex items-center gap-2 px-4 py-1.5 rounded-lg bg-teal-600 hover:bg-teal-500 text-white text-xs font-bold transition-all disabled:opacity-50 active:scale-95 cursor-pointer"
                >
                  <Database className="w-3.5 h-3.5" />
                  {isDemoSeeding ? "Yükleniyor..." : "Demo Sensörleri Yükle"}
                </button>
              )
            ) : (
              <button
                onClick={handleSeedDemo}
                disabled={isSeeding}
                className="flex items-center gap-2 px-4 py-1.5 rounded-lg bg-indigo-600 hover:bg-indigo-500 text-white text-xs font-bold transition-all disabled:opacity-50 active:scale-95 cursor-pointer"
              >
                <Database className="w-3.5 h-3.5" />
                {isSeeding ? "Kuruluyor..." : "Demo Kurulumu (Seed)"}
              </button>
            )
          )}

          {user ? (
            <button
              onClick={handleLogout}
              className="flex items-center gap-1.5 px-3.5 py-1.5 rounded-lg border border-slate-800 bg-slate-900 hover:bg-slate-800/80 text-slate-300 text-xs font-bold transition-all active:scale-95 cursor-pointer"
            >
              Çıkış Yap
            </button>
          ) : (
            <button
              onClick={() => setIsGuestMode(false)}
              className="flex items-center gap-1.5 px-3.5 py-1.5 rounded-lg bg-teal-500 hover:bg-teal-400 text-slate-950 text-xs font-bold transition-all active:scale-95 cursor-pointer hover:shadow-lg hover:shadow-teal-500/10"
            >
              Portal Girişi Yap &rarr;
            </button>
          )}
        </div>
      </header>

      {/* SignalR Connection Status Banners */}
      {hubStatus === "Reconnecting" && (
        <div className="z-20 bg-amber-500/15 border-b border-amber-500/30 text-amber-300 px-6 py-3 flex items-center gap-3 animate-pulse">
          <RefreshCw className="w-4.5 h-4.5 text-amber-400 animate-spin" />
          <div className="flex-1">
            <span className="font-bold text-xs sm:text-sm">Bağlantı Kesildi, Yeniden Bağlanılıyor...</span>
            <p className="text-[10px] sm:text-xs text-amber-400/80">Arka planda WebSocket bağlantısı otomatik olarak tazeleniyor. Lütfen bekleyin.</p>
          </div>
        </div>
      )}

      {hubStatus === "Disconnected" && (
        <div className="z-20 bg-rose-500/15 border-b border-rose-500/30 text-rose-300 px-6 py-3 flex items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <WifiOff className="w-4.5 h-4.5 text-rose-400" />
            <div>
              <span className="font-bold text-xs sm:text-sm">Bağlantı Çevrimdışı (Offline)</span>
              <p className="text-[10px] sm:text-xs text-rose-400/80">API sunucusuna bağlanılamadı. Canlı veri akışı durduruldu.</p>
            </div>
          </div>
          <button
            onClick={() => window.location.reload()}
            className="flex items-center gap-1.5 px-3 py-1 rounded bg-rose-600 hover:bg-rose-500 text-white text-xs font-bold transition-all active:scale-95 whitespace-nowrap"
          >
            <RefreshCw className="w-3.5 h-3.5" />
            Yeniden Bağlan
          </button>
        </div>
      )}

      {hubStatus === "Error" && (
        <div className="z-20 bg-rose-500/15 border-b border-rose-500/30 text-rose-300 px-6 py-3 flex items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <AlertTriangle className="w-4.5 h-4.5 text-rose-400" />
            <div>
              <span className="font-bold text-xs sm:text-sm">Sunucu Bağlantı Hatası</span>
              <p className="text-[10px] sm:text-xs text-rose-400/80">{errorMessage || "API sunucusu yanıt vermiyor."}</p>
            </div>
          </div>
          <button
            onClick={() => window.location.reload()}
            className="flex items-center gap-1.5 px-3 py-1 rounded bg-rose-600 hover:bg-rose-500 text-white text-xs font-bold transition-all active:scale-95 whitespace-nowrap"
          >
            <RefreshCw className="w-3.5 h-3.5" />
            Sayfayı Yenile
          </button>
        </div>
      )}

      {/* Flashing SIEM Security Alerts Panel */}
      {alerts.length > 0 && (
        <div className="mx-6 mt-6 p-4 rounded-xl bg-rose-950/20 border border-rose-500/30 backdrop-blur-sm animate-pulse z-10">
          <div className="flex items-center gap-2 text-rose-400 font-black text-sm mb-2 uppercase tracking-wider">
            <ShieldAlert className="w-5 h-5 text-rose-500" />
            SIEM Uyarısı: Güvenlik İhlal Girişimi Saptandı! 🚨
          </div>
          <div className="max-h-[100px] overflow-y-auto space-y-1.5 pr-2">
            {alerts.map((alert) => (
              <div key={alert.id} className="text-xs text-rose-300 font-mono flex items-start gap-2">
                <span className="text-rose-500">[{alert.timestamp}]</span>
                <span><strong>{alert.deviceSerialNumber}</strong>: {alert.message} ({alert.action})</span>
              </div>
            ))}
          </div>
          <div className="mt-3 flex justify-end">
            <button
              onClick={() => setAlerts([])}
              className="text-[10px] text-rose-400 hover:text-rose-200 font-bold uppercase underline cursor-pointer"
            >
              Uyarılardan Çık / Temizle
            </button>
          </div>
        </div>
      )}

      {/* Main Grid Content */}
      <div className="flex-1 grid grid-cols-1 xl:grid-cols-3 gap-6 p-6 z-10">
        
        {/* Left Column: Visual Dashboard Cards */}
        <div className="xl:col-span-2 space-y-6">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            {user && !isUserDevicesLoaded ? (
              <div className="col-span-full py-16 flex flex-col items-center justify-center space-y-4">
                <RefreshCw className="w-8 h-8 text-teal-400 animate-spin" />
                <span className="text-xs text-slate-500 font-mono">Sensör listesi güncelleniyor... 🔌</span>
              </div>
            ) : user && userDevicesList.length === 0 ? (
              <div className="col-span-full p-8 rounded-3xl bg-slate-900/40 border border-slate-800/80 backdrop-blur-xl flex flex-col items-center justify-center text-center space-y-6">
                <div className="p-4 rounded-2xl bg-gradient-to-tr from-teal-500 to-indigo-500 text-slate-950 shadow-lg animate-pulse">
                  <Cpu className="w-8 h-8" />
                </div>
                <div className="max-w-md space-y-2">
                  <h3 className="text-lg font-bold text-slate-100">Sisteminizde Sensör Bulunmuyor 🔌</h3>
                  <p className="text-xs text-slate-400 leading-relaxed">
                    OmniPulse platformunu kendi kiracınız (şirketiniz) altında test etmek için örnek IoT sensörleri, hiyerarşik varlıklar (Assets) ve canlı geçmiş telemetri verilerini içeren demo veri setini anında kurabilirsiniz.
                  </p>
                </div>
                
                <div className="flex flex-col sm:flex-row gap-3 pt-2">
                  <button
                    onClick={handleSeedIoTDemo}
                    disabled={isDemoSeeding}
                    className="flex items-center justify-center gap-2 px-6 py-2.5 rounded-xl bg-teal-500 hover:bg-teal-400 text-slate-950 font-bold text-xs tracking-wide shadow-lg hover:shadow-teal-500/10 active:scale-95 transition-all disabled:opacity-50 cursor-pointer"
                  >
                    <Database className="w-4 h-4" />
                    {isDemoSeeding ? "Kuruluyor..." : "Demo Sensörleri Yükle (Seed)"}
                  </button>
                  <button
                    onClick={() => alert("Gerçek cihaz eklemek için 'api/iot/devices' endpoint'ini POST isteği ile veya backend envanter sisteminden 'CreateDevice' komutunu kullanarak gerçekleştirebilirsiniz.")}
                    className="flex items-center justify-center gap-2 px-6 py-2.5 rounded-xl border border-slate-800 bg-slate-950 hover:bg-slate-900 text-slate-300 font-bold text-xs tracking-wide active:scale-95 transition-all cursor-pointer"
                  >
                    <Settings className="w-4 h-4 text-slate-400" />
                    Gerçek Cihaz Bağla
                  </button>
                </div>
              </div>
            ) : (user ? userDevicesList : SEED_DEVICES).map((dev) => {
              const devData = devices[dev.serialNumber] || { lastTemp: 25, lastPress: 1000, isOnline: true, history: [] };
              const history = devData.history || [];

              return (
                <div key={dev.serialNumber} className={`relative p-5 rounded-2xl bg-slate-900/40 border transition-all backdrop-blur-sm group overflow-hidden ${
                  devData.isOnline 
                    ? "border-slate-800/80 hover:border-slate-700/80" 
                    : "border-slate-950 bg-slate-950/20 opacity-60"
                }`}>
                  {/* Decorative corner indicator */}
                  <div className="absolute top-0 right-0 w-24 h-24 bg-gradient-to-bl from-teal-500/5 to-transparent rounded-bl-full pointer-events-none" />

                  <div className="flex items-start justify-between">
                    <div>
                      <div className="flex items-center gap-2">
                        <span className={`w-2.5 h-2.5 rounded-full ${devData.isOnline ? "bg-emerald-500 shadow-[0_0_8px_rgba(16,185,129,0.6)]" : "bg-slate-700"}`} />
                        <div className="text-xs font-mono text-slate-500 tracking-wider uppercase">{dev.serialNumber}</div>
                      </div>
                      <h3 className="text-base font-bold text-slate-200 group-hover:text-teal-400 transition-colors mt-0.5">{dev.name}</h3>
                    </div>
                    <div className="p-2 rounded-lg bg-slate-950 border border-slate-800/50">
                      {dev.serialNumber.toLowerCase().includes("water") ? (
                        <Droplet className="w-5 h-5 text-sky-400" />
                      ) : dev.serialNumber.toLowerCase().includes("temp") ? (
                        <Flame className="w-5 h-5 text-amber-500" />
                      ) : dev.serialNumber.toLowerCase().includes("vib") ? (
                        <Activity className="w-5 h-5 text-emerald-400" />
                      ) : (
                        <Settings className="w-5 h-5 text-purple-400" />
                      )}
                    </div>
                  </div>

                  <div className="grid grid-cols-2 gap-4 mt-6">
                    <div className="bg-slate-950/60 border border-slate-900 p-3 rounded-xl">
                      <div className="text-[10px] font-mono text-slate-500">Gelen Değer</div>
                      <div className="text-xl font-black text-slate-100 font-mono mt-1 flex items-baseline gap-1">
                        {devData.isOnline ? devData.lastTemp.toFixed(1) : "---"}
                        <span className="text-xs font-normal text-slate-400">{devData.isOnline ? dev.unit : ""}</span>
                      </div>
                    </div>
                    <div className="bg-slate-950/60 border border-slate-900 p-3 rounded-xl">
                      <div className="text-[10px] font-mono text-slate-500">Sistem Basıncı</div>
                      <div className="text-xl font-black text-slate-100 font-mono mt-1 flex items-baseline gap-1">
                        {devData.isOnline ? devData.lastPress.toFixed(0) : "---"}
                        <span className="text-xs font-normal text-slate-400">{devData.isOnline ? "hPa" : ""}</span>
                      </div>
                    </div>
                  </div>

                  {/* SVG Real-time sparkline graph */}
                  <div className="mt-5 h-16 w-full relative bg-slate-950/30 rounded-xl overflow-hidden border border-slate-900/50">
                    {!devData.isOnline ? (
                      <div className="absolute inset-0 flex items-center justify-center text-[10px] font-mono text-rose-500/80 bg-rose-950/5">
                        <WifiOff className="w-4 h-4 mr-1.5" /> Cihaz Çevrimdışı (OFFLINE)
                      </div>
                    ) : history.length > 1 ? (
                      <svg className="w-full h-full" viewBox="0 0 200 60" preserveAspectRatio="none">
                        <defs>
                          <linearGradient id={`grad-${dev.serialNumber}`} x1="0" y1="0" x2="0" y2="1">
                            <stop offset="0%" stopColor="#2dd4bf" stopOpacity="0.25" />
                            <stop offset="100%" stopColor="#2dd4bf" stopOpacity="0" />
                          </linearGradient>
                        </defs>
                        <path
                          d={`${generateSvgPath(history, "temp", 200, 60)} L 200,60 L 0,60 Z`}
                          fill={`url(#grad-${dev.serialNumber})`}
                        />
                        <path
                          d={generateSvgPath(history, "temp", 200, 60)}
                          fill="none"
                          stroke="#2dd4bf"
                          strokeWidth="2"
                          strokeLinecap="round"
                        />
                      </svg>
                    ) : (
                      <div className="absolute inset-0 flex items-center justify-center text-[10px] font-mono text-slate-600">
                        Canlı veri akışı bekleniyor...
                      </div>
                    )}
                  </div>
                </div>
              );
            })}
          </div>

          {/* Setup / Database Configuration Log Display */}
          {seedLogs && (
            <div className="p-5 rounded-2xl bg-slate-900/20 border border-slate-800/60 backdrop-blur-sm">
              <div className="flex items-center justify-between mb-3">
                <div className="flex items-center gap-2 text-xs font-bold text-slate-400 uppercase tracking-wide">
                  <Database className="w-4 h-4 text-indigo-400" />
                  Veri Tabanı Seeding Çıktısı
                </div>
                <button
                  onClick={() => setSeedLogs(null)}
                  className="text-[10px] font-mono text-slate-500 hover:text-slate-300"
                >
                  Gizle
                </button>
              </div>
              <pre className="text-xs font-mono bg-slate-950 p-4 rounded-xl border border-slate-900 overflow-x-auto text-indigo-300/90 leading-relaxed max-h-[160px] overflow-y-auto">
                {seedLogs}
              </pre>
            </div>
          )}

          {/* Ekip & Davet Yönetimi Paneli 👥 (Sadece Tenant Owner ve Adminler için) */}
          {user && user.roles && user.roles.some(r => r.toLowerCase() === "owner" || r.toLowerCase() === "admin") && (
            <div className="p-6 rounded-2xl bg-gradient-to-br from-slate-900/40 via-slate-900/30 to-teal-950/10 border border-slate-800/80 backdrop-blur-sm space-y-6 animate-fade-in">
              
              {/* Card Header */}
              <div className="flex items-center gap-3 border-b border-slate-800/60 pb-4">
                <div className="p-2 rounded-lg bg-teal-500/10 border border-teal-500/20 text-teal-400">
                  <UserPlus className="w-5 h-5" />
                </div>
                <div>
                  <h3 className="text-base font-bold text-slate-200">Ekip & Şirket Yetki Yönetimi</h3>
                  <p className="text-xs text-slate-500 font-mono">Çalışanları ve sürücüleri şirketinize davet edin.</p>
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                
                {/* Left Side: Tenant status details */}
                <div className="space-y-4">
                  <h4 className="text-xs font-bold text-slate-400 uppercase tracking-wider flex items-center gap-1.5">
                    <ShieldCheck className="w-4 h-4 text-emerald-400" />
                    Şirket Sistem Bağlamı (BFF)
                  </h4>
                  
                  {tenantStatus ? (
                    <div className="bg-slate-950/60 border border-slate-900 p-4 rounded-xl space-y-3 font-mono text-xs">
                      <div>
                        <span className="text-slate-500 block text-[10px] uppercase">Mevcut Şirket (Tenant)</span>
                        <span className="text-slate-200 font-bold">{tenantStatus.tenantName}</span>
                      </div>
                      <div>
                        <span className="text-slate-500 block text-[10px] uppercase">Şirket Kodu (Identifier)</span>
                        <span className="text-teal-400 font-bold">@{tenantStatus.detectedTenant}</span>
                      </div>
                      <div>
                        <span className="text-slate-500 block text-[10px] uppercase">Aktif Veritabanı Hattı</span>
                        <span className="text-indigo-400 text-[10px] break-all block">{tenantStatus.activeConnectionString}</span>
                      </div>
                      <div className="pt-2 border-t border-slate-900 flex items-center gap-2">
                        <span className="w-2 h-2 rounded-full bg-emerald-500 animate-pulse" />
                        <span className="text-slate-500 text-[10px]">Tünel Modül Aktif</span>
                      </div>
                    </div>
                  ) : (
                    <div className="flex items-center gap-2 text-xs text-slate-500 font-mono py-6">
                      <RefreshCw className="w-3.5 h-3.5 animate-spin text-teal-400" />
                      Sistem detayları sorgulanıyor...
                    </div>
                  )}

                  <div className="text-[10px] text-slate-500 leading-relaxed bg-slate-950/20 p-3 rounded-xl border border-slate-900/50">
                    <strong>Güvenlik Zinciri Uyarısı:</strong> Dışarıdan doğrudan kayıt kapalıdır. Alt kullanıcıların sisteme girebilmesi için e-posta daveti gönderilmeli, davet alan kullanıcılar <code>/accept-invite</code> üzerinden hesaplarını aktif etmelidir.
                  </div>
                </div>

                {/* Right Side: Invite form */}
                <div>
                  <h4 className="text-xs font-bold text-slate-400 uppercase tracking-wider mb-4">
                    Yeni Üyelik Daveti Gönder
                  </h4>

                  <form onSubmit={handleSendInvite} className="space-y-4">
                    <div>
                      <label className="block text-[10px] font-bold text-slate-400 uppercase tracking-wide mb-1">E-Posta Adresi</label>
                      <input
                        type="email"
                        required
                        value={inviteEmail}
                        onChange={(e) => setInviteEmail(e.target.value)}
                        placeholder="surucu@omnipulse.com"
                        className="w-full px-4 py-2.5 rounded-xl bg-slate-950/80 border border-slate-800/60 focus:border-teal-500 focus:ring-1 focus:ring-teal-500 text-xs outline-none transition-all"
                      />
                    </div>

                    <div>
                      <label className="block text-[10px] font-bold text-slate-400 uppercase tracking-wide mb-1">Kullanıcı Rolü</label>
                      <select
                        value={inviteRole}
                        onChange={(e) => setInviteRole(e.target.value)}
                        className="w-full bg-slate-950 border border-slate-800 rounded-xl px-4 py-2.5 text-xs text-slate-200 focus:border-teal-500 focus:ring-1 focus:ring-teal-500 outline-none"
                      >
                        <option value="Member">Member (Sürücü / Standart Çalışan)</option>
                        <option value="Admin">Admin (Şirket Yöneticisi)</option>
                        <option value="Owner">Owner (Şirket Kurucusu / Tam Yetki)</option>
                      </select>
                    </div>

                    {inviteError && (
                      <div className="p-3 rounded-xl bg-rose-500/10 border border-rose-500/30 text-rose-400 text-[10px] font-mono">
                        {inviteError}
                      </div>
                    )}

                    {inviteSuccess && (
                      <div className="p-3 rounded-xl bg-emerald-500/10 border border-emerald-500/30 text-emerald-400 text-[10px] font-mono">
                        {inviteSuccess}
                      </div>
                    )}

                    <button
                      type="submit"
                      disabled={inviteLoading}
                      className="w-full py-2.5 rounded-xl bg-teal-500 hover:bg-teal-400 text-slate-950 font-bold text-xs tracking-wide transition-all active:scale-98 disabled:opacity-50 flex items-center justify-center gap-1.5 cursor-pointer"
                    >
                      {inviteLoading ? (
                        <>
                          <RefreshCw className="w-3.5 h-3.5 animate-spin" />
                          Davet Gönderiliyor...
                        </>
                      ) : (
                        <>
                          <UserPlus className="w-3.5 h-3.5" />
                          Şirkete Davet Et
                        </>
                      )}
                    </button>
                  </form>
                </div>
              </div>
            </div>
          )}

          {/* Interactive Pipeline & SIEM Simulator Panel */}
          {showSimulators && (
            <div className="p-6 rounded-2xl bg-gradient-to-br from-slate-900/50 via-slate-900/30 to-indigo-950/20 border border-slate-800/80 backdrop-blur-sm">
              <div className="flex flex-col sm:flex-row sm:items-center justify-between border-b border-slate-800/60 pb-4 mb-6 gap-3">
                <div className="flex items-center gap-2">
                  <Cpu className="w-5 h-5 text-teal-400" />
                  <h3 className="text-base font-bold text-slate-200">Kinesis & SIEM Güvenlik Simülatörü</h3>
                </div>

                {/* Loop simulation control */}
                <button
                  onClick={() => setIsAutoSim(!isAutoSim)}
                  className={`flex items-center gap-2 px-4 py-2 rounded-xl text-xs font-bold transition-all active:scale-95 self-end sm:self-auto ${
                    isAutoSim
                      ? "bg-amber-600 hover:bg-amber-500 text-white animate-pulse"
                      : "bg-teal-500 hover:bg-teal-400 text-slate-950"
                  }`}
                >
                  {isAutoSim ? (
                    <>
                      <Pause className="w-3.5 h-3.5" />
                      Oto-Simülatör Durdur
                    </>
                  ) : (
                    <>
                      <Play className="w-3.5 h-3.5" />
                      Oto-Simülatör Başlat (2s)
                    </>
                  )}
                </button>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                
                {/* Simulator Left Column: Connection Status & Malicious Actions */}
                <div className="space-y-5">
                  <div>
                    <label className="block text-xs font-bold text-slate-400 uppercase tracking-wide mb-2">Simüle Edilecek Sensör</label>
                    <select
                      value={selectedDevice}
                      onChange={(e) => setSelectedDevice(e.target.value)}
                      className="w-full bg-slate-950 border border-slate-800 rounded-xl px-4 py-3 text-sm text-slate-200 focus:border-teal-500 focus:ring-1 focus:ring-teal-500 outline-none"
                    >
                      {SEED_DEVICES.map((d) => (
                        <option key={d.serialNumber} value={d.serialNumber}>
                          [{d.serialNumber}] {d.name}
                        </option>
                      ))}
                      <option value="SN-MALICIOUS-999">🚨 [GÜVENLİK İHLALİ] Kayıt Dışı Cihaz SN-MALICIOUS-999</option>
                    </select>
                  </div>

                  {/* Connection Status Simulation (Online / Offline) */}
                  <div>
                    <label className="block text-xs font-bold text-slate-400 uppercase tracking-wide mb-2">Bağlantı Durumunu Değiştir</label>
                    <div className="grid grid-cols-2 gap-3">
                      <button
                        onClick={() => handleSendConnectionStatus(selectedDevice, true)}
                        disabled={selectedDevice === "SN-MALICIOUS-999"}
                        className="flex items-center justify-center gap-1.5 px-4 py-2.5 rounded-xl border border-emerald-500/30 bg-emerald-500/10 text-emerald-400 text-xs font-bold hover:bg-emerald-500/20 active:scale-95 transition-all disabled:opacity-30"
                      >
                        <Power className="w-3.5 h-3.5 text-emerald-400" />
                        Cihazı Aç (ONLINE)
                      </button>
                      <button
                        onClick={() => handleSendConnectionStatus(selectedDevice, false)}
                        disabled={selectedDevice === "SN-MALICIOUS-999"}
                        className="flex items-center justify-center gap-1.5 px-4 py-2.5 rounded-xl border border-slate-800 bg-slate-950 text-slate-400 text-xs font-bold hover:bg-slate-900 active:scale-95 transition-all disabled:opacity-30"
                      >
                        <Power className="w-3.5 h-3.5 text-slate-500" />
                        Cihazı Kapat (OFFLINE)
                      </button>
                    </div>
                  </div>

                  {/* Malicious Attack Simulation Button */}
                  {selectedDevice === "SN-MALICIOUS-999" && (
                    <div className="p-4 rounded-xl bg-rose-950/20 border border-rose-500/30 text-xs text-rose-300 font-mono">
                      <AlertTriangle className="w-4 h-4 text-rose-500 mb-1" />
                      Bu cihaz sistemde kayıtlı değildir. Bu cihazdan telemetri göndermeyi denediğinizde Kinesis hattı yetkisiz denemeyi yakalayacak ve yukarıda flashing bir SIEM Security uyarısı üretecektir.
                    </div>
                  )}
                </div>

                {/* Simulator Right Column: Sliders & Send Actions */}
                <div className="space-y-5">
                  <div>
                    <div className="flex items-center justify-between text-xs text-slate-400 font-bold uppercase mb-2">
                      <span>Telemetri Değeri (Sıcaklık/Seviye)</span>
                      <span className="text-teal-400 font-mono text-sm">{simTemp.toFixed(1)}</span>
                    </div>
                    <input
                      type="range"
                      min="0"
                      max="150"
                      step="0.5"
                      value={simTemp}
                      onChange={(e) => setSimTemp(Number(e.target.value))}
                      className="w-full accent-teal-400 bg-slate-800 rounded-lg appearance-none h-1.5 cursor-pointer"
                    />
                  </div>

                  <div>
                    <div className="flex items-center justify-between text-xs text-slate-400 font-bold uppercase mb-2">
                      <span>Sistem Atmosfer Basıncı (hPa)</span>
                      <span className="text-indigo-400 font-mono text-sm">{simPress.toFixed(0)}</span>
                    </div>
                    <input
                      type="range"
                      min="950"
                      max="1060"
                      step="1"
                      value={simPress}
                      onChange={(e) => setSimPress(Number(e.target.value))}
                      className="w-full accent-indigo-400 bg-slate-800 rounded-lg appearance-none h-1.5 cursor-pointer"
                    />
                  </div>

                  <button
                    onClick={() => handleSendTelemetry(selectedDevice, simTemp, simPress)}
                    disabled={isSending || isAutoSim}
                    className="w-full py-3 rounded-xl bg-gradient-to-r from-teal-500 to-indigo-500 text-slate-950 hover:from-teal-400 hover:to-indigo-400 disabled:opacity-50 text-sm font-black tracking-wide active:scale-98 transition-all flex items-center justify-center gap-2"
                  >
                    {isSending ? (
                      "Yükleniyor..."
                    ) : (
                      <>
                        <Send className="w-4 h-4" />
                        Kinesis&apos;e Telemetri Pompala (POST)
                      </>
                    )}
                  </button>
                </div>
              </div>
            </div>
          )}
        </div>

        {/* Right Column: WebSocket Live Terminal Log */}
        <div className="flex flex-col rounded-2xl bg-slate-900/30 border border-slate-800/80 backdrop-blur-sm overflow-hidden h-[600px] xl:h-auto">
          {/* Terminal Header */}
          <div className="p-4 bg-slate-950 border-b border-slate-900 flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Terminal className="w-4 h-4 text-teal-400" />
              <span className="text-xs font-bold text-slate-300 tracking-wider uppercase font-mono">Canlı Terminal Log (WebSocket)</span>
            </div>
            <button
              onClick={() => setLogs([])}
              className="text-[10px] font-mono text-slate-500 hover:text-slate-300"
            >
              Temizle
            </button>
          </div>

          {/* Terminal Body */}
          <div className="flex-1 bg-slate-950/80 p-4 font-mono text-xs overflow-y-auto space-y-2.5 max-h-[500px] xl:max-h-none scrollbar-thin scrollbar-thumb-slate-800">
            {logs.length === 0 ? (
              <div className="h-full flex flex-col items-center justify-center text-slate-600 text-center p-6">
                <Wifi className="w-8 h-8 text-slate-700 mb-2 animate-bounce" />
                <p className="font-mono">Kinesis hattından anlık telemetri bekleniyor...</p>
                <p className="text-[10px] text-slate-700 mt-1">
                  (Simülatörden veri pompalayarak akışı başlatabilirsiniz)
                </p>
              </div>
            ) : (
              logs.map((log) => (
                <div key={log.id} className="border-b border-slate-900/50 pb-2 leading-relaxed">
                  <div className="flex items-center justify-between text-[10px]">
                    <span className="text-slate-500">{log.timestamp}</span>
                    <span className="text-teal-400/80 font-bold px-1.5 py-0.5 rounded bg-teal-500/5 border border-teal-500/10 uppercase tracking-wide">
                      {log.deviceId}
                    </span>
                  </div>
                  <div className="mt-1 flex items-baseline gap-1.5">
                    <span className="text-indigo-400 font-bold">{log.key}:</span>
                    <span className="text-emerald-400 font-black font-mono text-sm">{log.value}</span>
                  </div>
                  <div className="text-[9px] text-slate-600 mt-1 truncate bg-slate-900/30 px-1 py-0.5 rounded">
                    Raw: {JSON.stringify(log.raw)}
                  </div>
                </div>
              ))
            )}
            <div ref={terminalEndRef} />
          </div>
        </div>

      </div>

      {/* Footer */}
      <footer className="z-10 border-t border-slate-900 bg-slate-950/80 py-4 px-6 text-center text-xs text-slate-600 font-mono">
        © 2026 OmniPulse Systems. Clean Architecture SaaS Module. Powered by AWS Kinesis Streams.
      </footer>
    </main>
  );
}