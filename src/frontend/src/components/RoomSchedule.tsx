import React from 'react';

interface OccupiedDate {
  CheckInDate: string;
  CheckOutDate: string;
}

interface Room {
  Id: number;
  RoomNumber: string;
  Type: string;
  Capacity: number;
  PricePerNight: number;
  OccupiedDates: OccupiedDate[];
}

interface RoomScheduleProps {
  rooms: Room[];
  formatDate: (dateString: string) => string;
}

const RoomSchedule: React.FC<RoomScheduleProps> = ({
  rooms,
  formatDate
}) => {
  return (
    <div>
      <h4 className="mb-4">Dostępność Pokoi</h4>
      <div className="row">
        {rooms.map(room => (
          <div className="col-md-6 mb-4" key={room.Id}>
            <div className="card h-100 border-light shadow-sm">
              <div className="card-header d-flex justify-content-between align-items-center">
                <strong>🚪 Pokój {room.RoomNumber}</strong>
                <span className="badge bg-secondary">{room.Type}</span>
              </div>
              <div className="card-body">
                <p className="card-text mb-2">
                  <small>Cena: {room.PricePerNight} PLN | Pojemność: {room.Capacity} os.</small>
                </p>
                
                <h6 className="mt-3 text-muted">Zajęte terminy:</h6>
                {room.OccupiedDates && room.OccupiedDates.length > 0 ? (
                  <ul className="list-group list-group-flush">
                    {room.OccupiedDates.map((d, index) => (
                      <li key={index} className="list-group-item list-group-item-danger py-1">
                        🔒 {formatDate(d.CheckInDate)} — {formatDate(d.CheckOutDate)}
                      </li>
                    ))}
                  </ul>
                ) : (
                  <div className="alert alert-success py-2 mb-0">✨ Obecnie wolny (brak przyszłych rezerwacji)</div>
                )}
              </div>
            </div>
          </div>
        ))}
        {rooms.length === 0 && (
          <div className="col-12">
            <div className="alert alert-info">Brak dostępnych pokoi do wyświetlenia.</div>
          </div>
        )}
      </div>
    </div>
  );
};

export default RoomSchedule;
