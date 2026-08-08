import React from 'react';

export function MainLayout({ children }: { children: React.ReactNode }) {
  return <div className="min-h-screen bg-[#F8FAFC]">{children}</div>;
}
