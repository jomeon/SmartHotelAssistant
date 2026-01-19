import React from 'react';

interface ReservationFormProps {
  formData: {
    guestName: string;
    guestEmail: string;
    roomId: string;
    checkInDate: string;
    checkOutDate: string;
  };
  rooms: {
    Id: number;
    RoomNumber: string;
    Type: string;
    Capacity: number;
    PricePerNight: number;
  }[];
  loading: boolean;
  onFormChange: (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => void;
  onSubmit: (e: React.FormEvent) => void;
}

const ReservationForm: React.FC<ReservationFormProps> = ({
  formData,
  rooms,
  loading,
  onFormChange,
  onSubmit
}) => {
  return (
    <form onSubmit={onSubmit}>
      <h4 className="mb-3">Zarezerwuj pobyt</h4>
      
      <div className="row">
        <div className="col-md-6 mb-3">
          <label className="form-label">Imię i Nazwisko</label>
          <input required type="text" className="form-control" 
            name="guestName"
            value={formData.guestName} 
            onChange={onFormChange} />
        </div>
        <div className="col-md-6 mb-3">
          <label className="form-label">E-mail</label>
          <input required type="email" className="form-control" 
            name="guestEmail"
            value={formData.guestEmail} 
            onChange={onFormChange} />
        </div>
      </div>

      <div className="mb-3">
        <label className="form-label">Wybierz Pokój</label>
        <select required className="form-select" 
          name="roomId"
          value={formData.roomId} 
          onChange={onFormChange}>
          <option value="">-- Wybierz z listy --</option>
          {rooms.map(room => (
            <option key={room.Id} value={room.Id}>
              Pokój {room.RoomNumber} ({room.Type}) - {room.PricePerNight} PLN/noc
            </option>
          ))}
        </select>
      </div>

      <div className="row">
        <div className="col-md-6 mb-3">
          <label className="form-label">Od</label>
          <input required type="date" className="form-control" 
            name="checkInDate"
            value={formData.checkInDate} 
            onChange={onFormChange} />
        </div>
        <div className="col-md-6 mb-3">
          <label className="form-label">Do</label>
          <input required type="date" className="form-control" 
            name="checkOutDate"
            value={formData.checkOutDate} 
            onChange={onFormChange} />
        </div>
      </div>

      <button type="submit" className="btn btn-primary w-100" disabled={loading}>
        {loading ? 'Przetwarzanie...' : 'Zatwierdź Rezerwację'}
      </button>
    </form>
  );
};

export default ReservationForm;
