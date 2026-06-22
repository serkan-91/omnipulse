import { cookies } from "next/headers";

const BACKEND_URL = "http://localhost:5294";

export function parseJwt(token: string) {
  try {
    const base64Url = token.split('.')[1];
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split('')
        .map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    );
    return JSON.parse(jsonPayload);
  } catch (e) {
    return null;
  }
}

export async function getOrRefreshAccessToken(): Promise<string | null> {
  const cookieStore = await cookies();
  let token = cookieStore.get("op_access_token")?.value;
  const refreshToken = cookieStore.get("op_refresh_token")?.value;

  if (!token) {
    return null;
  }

  const payload = parseJwt(token);
  if (!payload) {
    return null;
  }

  const exp = payload.exp;
  // Expired or close to expiration (within 10 seconds)
  const isExpired = exp && (Date.now() / 1000) >= (exp - 10);

  if (isExpired && refreshToken) {
    console.log("[BFF] Access token expired or expiring. Attempting refresh...");
    try {
      const response = await fetch(`${BACKEND_URL}/api/auth/refresh`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          token: token,
          refreshToken: refreshToken
        })
      });

      if (response.ok) {
        const data = await response.json();
        if (data.isSuccess && data.token) {
          token = data.token;
          
          // Update cookies in the response
          cookieStore.set("op_access_token", token!, {
            httpOnly: true,
            secure: process.env.NODE_ENV === "production",
            sameSite: "strict",
            path: "/",
            maxAge: 60 * 60 * 2, // 2 hours
          });

          if (data.refreshToken) {
            cookieStore.set("op_refresh_token", data.refreshToken, {
              httpOnly: true,
              secure: process.env.NODE_ENV === "production",
              sameSite: "strict",
              path: "/",
              maxAge: 60 * 60 * 24 * 7, // 7 days
            });
          }
          console.log("[BFF] Token refreshed successfully via C# API.");
        } else {
          console.warn("[BFF] Refresh token call succeeded but returned failure:", data.message);
        }
      } else {
        console.error("[BFF] Refresh token call failed with status:", response.status);
      }
    } catch (err) {
      console.error("[BFF] Error trying to refresh token:", err);
    }
  }

  return token ?? null;
}
