import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Plus, Edit, Trash2, Loader2, X } from 'lucide-react';
import { Button, Card, CardContent, CardHeader, CardTitle, Input } from '../../components/ui';
import { formatCurrency, formatDuration } from '../../lib/utils';
import { servicesApi, type ServiceDto, type CreateServiceRequest, type UpdateServiceRequest } from '../../api/services';

interface ServiceFormData {
  name: string;
  description: string;
  durationMinutes: number;
  price: number;
  capacity: number;
  bufferMinutes: number;
  color: string;
}

const defaultFormData: ServiceFormData = {
  name: '',
  description: '',
  durationMinutes: 30,
  price: 0,
  capacity: 1,
  bufferMinutes: 0,
  color: '#6366f1',
};

const colorOptions = [
  '#6366f1', '#8b5cf6', '#ec4899', '#ef4444', '#f97316',
  '#f59e0b', '#84cc16', '#22c55e', '#10b981', '#14b8a6',
  '#06b6d4', '#0ea5e9', '#3b82f6',
];

export function ServicesPage() {
  const queryClient = useQueryClient();
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingService, setEditingService] = useState<ServiceDto | null>(null);
  const [formData, setFormData] = useState<ServiceFormData>(defaultFormData);
  const [deleteConfirm, setDeleteConfirm] = useState<string | null>(null);

  const { data: services, isLoading } = useQuery({
    queryKey: ['services'],
    queryFn: () => servicesApi.getAll(),
  });

  const createMutation = useMutation({
    mutationFn: (data: CreateServiceRequest) => servicesApi.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['services'] });
      closeModal();
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateServiceRequest }) =>
      servicesApi.update(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['services'] });
      closeModal();
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => servicesApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['services'] });
      setDeleteConfirm(null);
    },
  });

  const openCreateModal = () => {
    setEditingService(null);
    setFormData(defaultFormData);
    setIsModalOpen(true);
  };

  const openEditModal = (service: ServiceDto) => {
    setEditingService(service);
    setFormData({
      name: service.name,
      description: service.description || '',
      durationMinutes: service.durationMinutes,
      price: service.price,
      capacity: service.capacity,
      bufferMinutes: 0,
      color: service.color || '#6366f1',
    });
    setIsModalOpen(true);
  };

  const closeModal = () => {
    setIsModalOpen(false);
    setEditingService(null);
    setFormData(defaultFormData);
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    if (editingService) {
      updateMutation.mutate({
        id: editingService.id,
        data: {
          name: formData.name,
          description: formData.description || undefined,
          durationMinutes: formData.durationMinutes,
          price: formData.price,
          capacity: formData.capacity,
          bufferMinutes: formData.bufferMinutes,
          color: formData.color,
        },
      });
    } else {
      createMutation.mutate({
        name: formData.name,
        description: formData.description || undefined,
        durationMinutes: formData.durationMinutes,
        price: formData.price,
        capacity: formData.capacity,
        bufferMinutes: formData.bufferMinutes,
        color: formData.color,
      });
    }
  };

  const isSubmitting = createMutation.isPending || updateMutation.isPending;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Services</h1>
          <p className="text-gray-500 dark:text-gray-400">
            Manage the services your business offers
          </p>
        </div>
        <Button onClick={openCreateModal}>
          <Plus className="h-4 w-4 mr-2" />
          Add Service
        </Button>
      </div>

      <Card>
        <CardContent className="p-0">
          {isLoading ? (
            <div className="flex items-center justify-center py-12">
              <Loader2 className="h-8 w-8 animate-spin text-primary-600" />
            </div>
          ) : services && services.length > 0 ? (
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="border-b dark:border-gray-700">
                    <th className="text-left p-4 font-medium text-gray-500 dark:text-gray-400">
                      Service
                    </th>
                    <th className="text-left p-4 font-medium text-gray-500 dark:text-gray-400">
                      Duration
                    </th>
                    <th className="text-left p-4 font-medium text-gray-500 dark:text-gray-400">
                      Price
                    </th>
                    <th className="text-left p-4 font-medium text-gray-500 dark:text-gray-400">
                      Type
                    </th>
                    <th className="text-right p-4 font-medium text-gray-500 dark:text-gray-400">
                      Actions
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {services.map((service) => (
                    <tr key={service.id} className="border-b dark:border-gray-700 last:border-0">
                      <td className="p-4">
                        <div className="flex items-center gap-3">
                          <div
                            className="h-3 w-3 rounded-full"
                            style={{ backgroundColor: service.color || '#6366f1' }}
                          />
                          <div>
                            <span className="font-medium text-gray-900 dark:text-white">
                              {service.name}
                            </span>
                            {service.description && (
                              <p className="text-sm text-gray-500">{service.description}</p>
                            )}
                          </div>
                        </div>
                      </td>
                      <td className="p-4 text-gray-600 dark:text-gray-300">
                        {formatDuration(service.durationMinutes)}
                      </td>
                      <td className="p-4 text-gray-600 dark:text-gray-300">
                        {formatCurrency(service.price)}
                      </td>
                      <td className="p-4">
                        {service.isGroupClass ? (
                          <span className="px-2 py-1 text-xs rounded-full bg-purple-100 text-purple-700 dark:bg-purple-900 dark:text-purple-300">
                            Group ({service.capacity} max)
                          </span>
                        ) : (
                          <span className="px-2 py-1 text-xs rounded-full bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300">
                            Private
                          </span>
                        )}
                      </td>
                      <td className="p-4">
                        <div className="flex items-center justify-end gap-2">
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => openEditModal(service)}
                          >
                            <Edit className="h-4 w-4" />
                          </Button>
                          {deleteConfirm === service.id ? (
                            <div className="flex items-center gap-1">
                              <Button
                                variant="ghost"
                                size="sm"
                                onClick={() => deleteMutation.mutate(service.id)}
                                disabled={deleteMutation.isPending}
                              >
                                {deleteMutation.isPending ? (
                                  <Loader2 className="h-4 w-4 animate-spin" />
                                ) : (
                                  <span className="text-red-600 text-xs">Confirm</span>
                                )}
                              </Button>
                              <Button
                                variant="ghost"
                                size="sm"
                                onClick={() => setDeleteConfirm(null)}
                              >
                                <X className="h-4 w-4" />
                              </Button>
                            </div>
                          ) : (
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => setDeleteConfirm(service.id)}
                            >
                              <Trash2 className="h-4 w-4 text-red-500" />
                            </Button>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : (
            <div className="text-center py-12 text-gray-500">
              <p>No services yet</p>
              <p className="text-sm">Add your first service to get started</p>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Modal */}
      {isModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center">
          <div
            className="absolute inset-0 bg-black/50"
            onClick={closeModal}
          />
          <Card className="relative z-10 w-full max-w-lg mx-4">
            <CardHeader className="flex flex-row items-center justify-between">
              <CardTitle>
                {editingService ? 'Edit Service' : 'Add Service'}
              </CardTitle>
              <Button variant="ghost" size="sm" onClick={closeModal}>
                <X className="h-4 w-4" />
              </Button>
            </CardHeader>
            <CardContent>
              <form onSubmit={handleSubmit} className="space-y-4">
                <Input
                  label="Service Name *"
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                  placeholder="e.g., Haircut"
                  required
                />

                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                    Description
                  </label>
                  <textarea
                    value={formData.description}
                    onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500 dark:bg-gray-800 dark:border-gray-600 dark:text-white"
                    rows={2}
                    placeholder="Brief description of the service"
                  />
                </div>

                <div className="grid grid-cols-2 gap-4">
                  <Input
                    label="Duration (minutes) *"
                    type="number"
                    value={formData.durationMinutes}
                    onChange={(e) => setFormData({ ...formData, durationMinutes: parseInt(e.target.value) || 0 })}
                    min={5}
                    required
                  />
                  <Input
                    label="Price ($) *"
                    type="number"
                    value={formData.price}
                    onChange={(e) => setFormData({ ...formData, price: parseFloat(e.target.value) || 0 })}
                    min={0}
                    step={0.01}
                    required
                  />
                </div>

                <div className="grid grid-cols-2 gap-4">
                  <Input
                    label="Capacity"
                    type="number"
                    value={formData.capacity}
                    onChange={(e) => setFormData({ ...formData, capacity: parseInt(e.target.value) || 1 })}
                    min={1}
                  />
                  <Input
                    label="Buffer Time (min)"
                    type="number"
                    value={formData.bufferMinutes}
                    onChange={(e) => setFormData({ ...formData, bufferMinutes: parseInt(e.target.value) || 0 })}
                    min={0}
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                    Color
                  </label>
                  <div className="flex flex-wrap gap-2">
                    {colorOptions.map((color) => (
                      <button
                        key={color}
                        type="button"
                        onClick={() => setFormData({ ...formData, color })}
                        className={`h-8 w-8 rounded-full border-2 transition-transform ${
                          formData.color === color
                            ? 'border-gray-900 dark:border-white scale-110'
                            : 'border-transparent'
                        }`}
                        style={{ backgroundColor: color }}
                      />
                    ))}
                  </div>
                </div>

                {formData.capacity > 1 && (
                  <p className="text-sm text-purple-600 bg-purple-50 p-2 rounded">
                    This will be a group class with max {formData.capacity} participants
                  </p>
                )}

                <div className="flex justify-end gap-2 pt-4">
                  <Button type="button" variant="outline" onClick={closeModal}>
                    Cancel
                  </Button>
                  <Button type="submit" disabled={isSubmitting || !formData.name}>
                    {isSubmitting ? (
                      <>
                        <Loader2 className="h-4 w-4 animate-spin mr-2" />
                        {editingService ? 'Updating...' : 'Creating...'}
                      </>
                    ) : editingService ? (
                      'Update Service'
                    ) : (
                      'Create Service'
                    )}
                  </Button>
                </div>
              </form>
            </CardContent>
          </Card>
        </div>
      )}
    </div>
  );
}
