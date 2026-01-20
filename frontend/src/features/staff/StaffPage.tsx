import { useQuery } from '@tanstack/react-query';
import { User, Loader2, Calendar } from 'lucide-react';
import { Card, CardContent } from '../../components/ui';
import { staffApi } from '../../api/staff';

export function StaffPage() {
  const { data: staff, isLoading } = useQuery({
    queryKey: ['staff'],
    queryFn: () => staffApi.getAll(),
  });

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Staff</h1>
        <p className="text-gray-500 dark:text-gray-400">
          View your team members. Manage staff in the Users section.
        </p>
      </div>

      {isLoading ? (
        <div className="flex items-center justify-center py-12">
          <Loader2 className="h-8 w-8 animate-spin text-primary-600" />
        </div>
      ) : staff && staff.length > 0 ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {staff.map((member) => (
            <Card key={member.id}>
              <CardContent className="p-6">
                <div className="flex items-start gap-4">
                  <div className="h-16 w-16 rounded-full bg-primary-100 flex items-center justify-center flex-shrink-0">
                    {member.avatarUrl ? (
                      <img
                        src={member.avatarUrl}
                        alt={member.fullName}
                        className="h-16 w-16 rounded-full object-cover"
                      />
                    ) : (
                      <User className="h-8 w-8 text-primary-600" />
                    )}
                  </div>
                  <div className="flex-1 min-w-0">
                    <h3 className="font-semibold text-gray-900 dark:text-white truncate">
                      {member.fullName}
                    </h3>
                    {member.title && (
                      <p className="text-sm text-gray-500 dark:text-gray-400">
                        {member.title}
                      </p>
                    )}
                    {member.bio && (
                      <p className="text-sm text-gray-400 mt-2 line-clamp-2">
                        {member.bio}
                      </p>
                    )}
                    <div className="flex items-center gap-2 mt-3">
                      <span className="inline-flex items-center px-2 py-1 text-xs rounded-full bg-green-100 text-green-700 dark:bg-green-900 dark:text-green-300">
                        <Calendar className="h-3 w-3 mr-1" />
                        {member.serviceIds.length} services
                      </span>
                    </div>
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      ) : (
        <Card>
          <CardContent className="py-12 text-center">
            <User className="h-12 w-12 mx-auto mb-4 text-gray-400" />
            <p className="text-gray-500">No staff members yet</p>
            <p className="text-sm text-gray-400">
              Add staff members in the Users section
            </p>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
