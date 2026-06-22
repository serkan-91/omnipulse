import { NextResponse } from "next/server";
import { getOrRefreshAccessToken } from "../../utils";

if (process.env.NODE_ENV === "development") {
  process.env.NODE_TLS_REJECT_UNAUTHORIZED = "0";
}

const BACKEND_URL = "https://localhost:7122";

async function handleProxy(request: Request, context: { params: Promise<{ path?: string[] }> }) {
  try {
    const token = await getOrRefreshAccessToken();
    const resolvedParams = await context.params;
    const pathSegments = resolvedParams.path || [];
    const apiPath = pathSegments.join("/");

    // Reconstruct query parameters
    const url = new URL(request.url);
    const searchParams = url.search;
    const backendUrl = `${BACKEND_URL}/${apiPath}${searchParams}`;

    // Read headers
    const headers = new Headers();
    
    // Copy headers (filtering host)
    request.headers.forEach((val, key) => {
      if (key.toLowerCase() !== "host" && key.toLowerCase() !== "cookie") {
        headers.set(key, val);
      }
    });

    // Attach authorization header if token is present
    if (token) {
      headers.set("Authorization", `Bearer ${token}`);
    }

    // Read body if method is not GET or HEAD
    let body: ArrayBuffer | undefined = undefined;
    if (request.method !== "GET" && request.method !== "HEAD") {
      body = await request.arrayBuffer();
    }

    const backendRes = await fetch(backendUrl, {
      method: request.method,
      headers: headers,
      body: body,
    });

    // Check if unauthorized, meaning token was invalid/revoked
    if (backendRes.status === 401) {
      return NextResponse.json({
        isSuccess: false,
        message: "Oturumunuz sonlandırılmış veya geçersiz."
      }, { status: 401 });
    }

    // Get response body
    const responseBody = await backendRes.arrayBuffer();

    // Create response
    const responseHeaders = new Headers();
    const backendContentType = backendRes.headers.get("content-type");
    if (backendContentType) {
      responseHeaders.set("content-type", backendContentType);
    }

    return new NextResponse(responseBody, {
      status: backendRes.status,
      statusText: backendRes.statusText,
      headers: responseHeaders,
    });
  } catch (error: unknown) {
    console.error("BFF Proxy Error:", error);
    return NextResponse.json({
      isSuccess: false,
      message: "BFF Proxy Hatası: Sunucuyla bağlantı kurulamadı."
    }, { status: 502 });
  }
}

export async function GET(request: Request, context: { params: Promise<{ path?: string[] }> }) {
  return handleProxy(request, context);
}

export async function POST(request: Request, context: { params: Promise<{ path?: string[] }> }) {
  return handleProxy(request, context);
}

export async function PUT(request: Request, context: { params: Promise<{ path?: string[] }> }) {
  return handleProxy(request, context);
}

export async function DELETE(request: Request, context: { params: Promise<{ path?: string[] }> }) {
  return handleProxy(request, context);
}

export async function PATCH(request: Request, context: { params: Promise<{ path?: string[] }> }) {
  return handleProxy(request, context);
}
