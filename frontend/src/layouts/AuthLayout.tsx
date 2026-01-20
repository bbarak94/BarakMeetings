import { Outlet } from 'react-router-dom';
import { Calendar } from 'lucide-react';

export function AuthLayout() {
  return (
    <div className="min-h-screen flex">
      {/* Left side - Branding */}
      <div className="hidden lg:flex lg:w-1/2 bg-primary-600 items-center justify-center p-12">
        <div className="max-w-md text-white">
          <div className="flex items-center gap-3 mb-8">
            <Calendar className="h-12 w-12" />
            <span className="text-3xl font-bold">BookingPlatform</span>
          </div>
          <h1 className="text-4xl font-bold mb-4">
            Streamline Your Bookings
          </h1>
          <p className="text-lg text-primary-100">
            The all-in-one scheduling solution for fitness studios, salons, clinics, and more.
            Manage appointments, staff, and clients effortlessly.
          </p>
        </div>
      </div>

      {/* Right side - Auth form */}
      <div className="flex-1 flex items-center justify-center p-8">
        <div className="w-full max-w-md">
          <div className="lg:hidden flex items-center gap-2 mb-8 justify-center">
            <Calendar className="h-8 w-8 text-primary-600" />
            <span className="text-2xl font-bold text-gray-900 dark:text-white">
              BookingPlatform
            </span>
          </div>
          <Outlet />
        </div>
      </div>
    </div>
  );
}
