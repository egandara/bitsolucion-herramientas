# Databricks notebook source
# MAGIC %md
# MAGIC # BCI_010_Base_Normativa_Operaciones_Fines_Generales
# MAGIC **************************************************************************

# COMMAND ----------

# MAGIC %md
# MAGIC ## Información del Notebook

# COMMAND ----------

# MAGIC %md
# MAGIC ### Encabezado
# MAGIC ********************************************************************************************************************
# MAGIC * Nombre: BCI_010_Base_Normativa_Operaciones_Fines_Generales.ipynb
# MAGIC * Ruta: 
# MAGIC * Autor: BitSolucion Spa
# MAGIC * Ing. SW BCI: <alguien.de.ti@bci.cl> - Marcelo Lizama <marcelo.lizama@bci.cl> - Ivan Jara <ivan.jara@bci.cl>
# MAGIC * Fecha: 22/12/2025
# MAGIC * Descripción: Notebook encargado de generar la informacion de las operaciones con creditos para fines generales Tipo Activo 17
# MAGIC * Documentación: 
# MAGIC ********************************************************************************************************************

# COMMAND ----------

# MAGIC %md
# MAGIC ### Mantenciones
# MAGIC **************************************************************************
# MAGIC #### Mantención Nro: 
# MAGIC * Autor: <Nombre Autor> (<Empresa del Autor (Bci/Otra)>) - Ing. SW BCI: <Nombre Ing. SW BCI>
# MAGIC * Fecha: <dd/mm/yyyy> 
# MAGIC * Descripción: <Descripción de la mantención>      
# MAGIC ***************************************************************************

# COMMAND ----------

# MAGIC %md
# MAGIC ### Tablas Entrada y Salida
# MAGIC **************************************************************************
# MAGIC #### Tablas Entrada: 
# MAGIC * tmp_d00_deuda_act_ctg
# MAGIC * tmp_modelo_com_gr
# MAGIC * tmp_modelo_indiv
# MAGIC ***************************************************************************
# MAGIC #### Tablas Salida: 
# MAGIC * base_archivos_normativos (Activo 17 Tabla 89 Manual CMF)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Captura de Variable

# COMMAND ----------

# MAGIC %md
# MAGIC ### Crear Widgets para Capturar de Variables

# COMMAND ----------

dbutils.widgets.removeAll()

dbutils.widgets.text("fechaProcesoW","","01 Fecha Proceso:")
dbutils.widgets.text("db_platinumW","","02 platinum DB:")
dbutils.widgets.text("db_plat_tempW","","03 platinum temp db:")
dbutils.widgets.text("db_RiesgoCredW","","04 slv_RiesgoCred_RiesgoCredPer DB:")
dbutils.widgets.text("db_slv_ParametricasW","","05 Parametricas DB:")

dbutils.widgets.text("db_location_plat_tempW","","06 platinum temp db:")

# COMMAND ----------

# MAGIC %md
# MAGIC ### Asignar Objeto a Lectura de Widgets y Variables

# COMMAND ----------

fechaProcesoX = dbutils.widgets.get("fechaProcesoW")
spark.conf.set("bci.fechaProcesoX", fechaProcesoX)

db_platinumX = dbutils.widgets.get("db_platinumW")
spark.conf.set("bci.db_platinumX", db_platinumX)

db_plat_tempX = dbutils.widgets.get("db_plat_tempW")
spark.conf.set("bci.db_plat_tempX", db_plat_tempX)

db_RiesgoCredX = dbutils.widgets.get("db_RiesgoCredW")
spark.conf.set("bci.db_RiesgoCredX", db_RiesgoCredX)

db_slv_ParametricasX = dbutils.widgets.get("db_slv_ParametricasW")
spark.conf.set("bci.db_slv_ParametricasX", db_slv_ParametricasX)


db_location_plat_tempX = dbutils.widgets.get("db_location_plat_tempW")
spark.conf.set("bci.db_location_plat_tempX", db_location_plat_tempX)


print("Parámetros")
print("fechaProcesoX: " + fechaProcesoX)
print("db_platinumX: " + db_platinumX)
print("db_plat_tempX: " + db_plat_tempX)
print("db_RiesgoCredX: " + db_RiesgoCredX)
print("db_slv_ParametricasX: " + db_slv_ParametricasX)
print("db_location_plat_tempX: " + db_location_plat_tempX)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Carga de funciones

# COMMAND ----------

# MAGIC %run "../../Funciones"

# COMMAND ----------

# MAGIC %md
# MAGIC ### Validación de ingreso de parámetros

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Parametro Vacio

# COMMAND ----------

valida_param_vacio(fechaProcesoX,'fechaProcesoX')
valida_param_vacio(db_platinumX,'db_platinumX')
valida_param_vacio(db_plat_tempX,'db_plat_tempX')
valida_param_vacio(db_RiesgoCredX,'db_RiesgoCredX')
valida_param_vacio(db_slv_ParametricasX,'db_slv_ParametricasX')
valida_param_vacio(db_location_plat_tempX,'db_location_plat_tempX')

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Base de Datos

# COMMAND ----------

valida_bd(db_platinumX, 'db_platinumX')
valida_bd(db_plat_tempX, 'db_plat_tempX')
valida_bd(db_RiesgoCredX, 'db_RiesgoCredX')
valida_bd(db_slv_ParametricasX, 'db_slv_ParametricasX')

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Fecha Valida

# COMMAND ----------

valida_fecha_valida(fechaProcesoX, 'fechaProcesoX')

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Fecha Futura

# COMMAND ----------

valida_fecha_futura(fechaProcesoX, 'fechaProcesoX')

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Ruta 

# COMMAND ----------

valida_ruta(db_location_plat_tempX, 'db_location_plat_tempX')

# COMMAND ----------

# MAGIC %md
# MAGIC ### Asignacion Variables de fecha

# COMMAND ----------

ano = str(fechaProcesoX)[:4]
mes = str(fechaProcesoX)[4:][:2]
dia = str(fechaProcesoX)[6:][:2]
fechanormativo = str(ano+'-'+mes+'-'+dia)
fechacinta = str(dia+'-'+mes+'-'+ano)
anomes = str(ano+mes)
anomesdia = str(ano+mes+dia)
ano_mes_dia = str(ano+'-'+mes+'-01')

print("fecha_Formato1: " + ano)
print("fecha_Formato2: " + mes)
print("fecha_Formato3: " + dia)
print("fecha_Formato4: " + fechanormativo)
print("fecha_Formato5: " + fechacinta)
print("fecha_Formato6: " + anomes)
print("fecha_Formato7: " + anomesdia)
print("fecha_Formato8: " + ano_mes_dia)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Inicio de Logica

# COMMAND ----------

# MAGIC %md
# MAGIC ### Creacion Tablas Temporales

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla bci10_basenorma_tmp_cred_fines_generales

# COMMAND ----------

drop_cred_fines_generales = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.bci10_basenorma_tmp_cred_fines_generales"""

# COMMAND ----------

sql_safe(drop_cred_fines_generales)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"bci10_basenorma_tmp_cred_fines_generales", True)

# COMMAND ----------

create_cred_fines_generales = """CREATE TABLE """ + db_plat_tempX + """.bci10_basenorma_tmp_cred_fines_generales
(
  periodo_id                      INT, 				
  rut_cliente                     STRING, 				
  dv_cliente                      STRING, 				
  cod_ope_original                STRING, 			
  cod_num_operacion               STRING, 		
  cod_tip_cart                    STRING, 				
  cod_tipo_ope                    STRING, 				
  des_producto_ope                STRING, 			
  des_banca_ope                   STRING, 			
  cod_cartera_ope                 STRING,			
  mto_deuda_ope                   DECIMAL(30, 0),	   			
  num_dias_mora_ope               INT,		
  ind_cdet                      	STRING,					
  fec_ingreso_deteriodo_ope       STRING,
  cod_cctb                      	STRING, 					
  fl_ope_reneg                  	STRING,
  tipo_activo               	    INT,
  nombre_activo             	    STRING,
  pi_cli                   	      STRING,
  pdi_cli                   	    STRING,
  pe_cli                   	      STRING,
  provision                   	  STRING,
  tipo_cli                   	    STRING,
  exposicion                      DECIMAL(30, 0),
  factor_expo                   	DECIMAL(30, 6),
  tip_gar                   	    STRING,
  mon_gar                         STRING,
  pvtg                   	        DECIMAL(30, 6),
  pi_metodo_interno               DECIMAL(30, 6),
  pdi_metodo_interno              DECIMAL(30, 6),
  pe_metodo_interno               DECIMAL(30, 6),
  prov_int                        DECIMAL(30, 0),
  monto_aval					            DECIMAL(30, 0)
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""bci10_basenorma_tmp_cred_fines_generales' """

# COMMAND ----------

sql_safe(create_cred_fines_generales)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla bci10_basenorma_tmp_cred_fines_generales_2

# COMMAND ----------

drop_cred_fines_generales_2 = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.bci10_basenorma_tmp_cred_fines_generales_2"""

# COMMAND ----------

sql_safe(drop_cred_fines_generales_2)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"bci10_basenorma_tmp_cred_fines_generales_2", True)

# COMMAND ----------

create_cred_fines_generales_2 = """CREATE TABLE """ + db_plat_tempX + """.bci10_basenorma_tmp_cred_fines_generales_2
(
  periodo_id                      INT, 				
  rut_cliente                     STRING, 				
  dv_cliente                      STRING, 				
  cod_ope_original                STRING, 			
  cod_num_operacion               STRING, 		
  cod_tip_cart                    STRING, 				
  cod_tipo_ope                    STRING, 				
  des_producto_ope                STRING, 			
  des_banca_ope                   STRING, 			
  cod_cartera_ope                 STRING,			
  mto_deuda_ope                   DECIMAL(30, 0),	   			
  num_dias_mora_ope               INT,		
  ind_cdet                      	STRING,					
  fec_ingreso_deteriodo_ope       STRING,
  cod_cctb                      	STRING, 					
  fl_ope_reneg                  	STRING,
  tipo_activo               	    INT,
  nombre_activo             	    STRING,
  pi_cli                   	      DECIMAL(30, 6),
  pdi_cli                   	    DECIMAL(30, 6),
  pe_cli                   	      DECIMAL(30, 7),
  provision                   	  DECIMAL(30, 0),
  tipo_cli                   	    STRING,
  exposicion                      DECIMAL(30, 0),
  factor_expo                   	DECIMAL(30, 6),
  tip_gar                   	    STRING,
  mon_gar                         DECIMAL(30, 0),
  pvtg                   	        DECIMAL(30, 6),
  pi_metodo_interno               DECIMAL(30, 6),
  pdi_metodo_interno              DECIMAL(30, 6),
  pe_metodo_interno               DECIMAL(30, 6),
  prov_int                        DECIMAL(30, 0),
  monto_aval					            DECIMAL(30, 0)
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""bci10_basenorma_tmp_cred_fines_generales_2' """

# COMMAND ----------

sql_safe(create_cred_fines_generales_2)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Desarrollo Logica

# COMMAND ----------

ult_dia_mes = ultimo_dia_habil(db_slv_ParametricasX, anomes)

ult_dia = ult_dia_mes.toPandas()
fec_proceso = ult_dia.iloc[0]["periodo_id"]

ano_proceso = str(fec_proceso)[:4]
mes_proceso = str(fec_proceso)[4:][:2]
dia_proceso = str(fec_proceso)[6:][:2]
date_proceso = str(ano_proceso+'-'+mes_proceso+'-'+dia_proceso)
date_proceso_2 = str(ano_proceso+mes_proceso+dia_proceso)

print("fecha_Formato9: " + date_proceso)
print("fecha_Formato10: " + date_proceso_2)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 1: Obtencion Univ Operaciones Creditos Fines Generales

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de las operaciones con credito fines generales desde tmp_d00_deuda_act_ctg y tmp_modelo_com_gr
# MAGIC
# MAGIC **Tabla de salida**: bci10_basenorma_tmp_cred_fines_generales (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM 				
# MAGIC - **rut_cliente**: Rut del cliente 				
# MAGIC - **dv_cliente**: Digito verificador del cliente			
# MAGIC - **cod_ope_original**: Operación Original (TRZ) 			
# MAGIC - **cod_num_operacion**: Número Operación 		
# MAGIC - **cod_tip_cart**: Tipo cartera 				
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00 				
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera 			
# MAGIC - **des_banca_ope**: Descripción operacion banca  			
# MAGIC - **cod_cartera_ope**: Subtipo cartera			
# MAGIC - **mto_deuda_ope**: Saldo total ifrs	   			
# MAGIC - **num_dias_mora_ope**: Dias de mora		
# MAGIC - **ind_cdet**: Indicador cartera deterioro  					
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación 
# MAGIC - **cod_cctb**: Cuenta contable					
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **tipo_activo**: Identificador del tipo activos
# MAGIC - **nombre_activo**: Nombre del tipo activo
# MAGIC - **pi_cli**: Porcentaje probabilidad de incumplimiento cliente
# MAGIC - **pdi_cli**: Porcentaje de perdida dada por el incumplimiento cliente
# MAGIC - **pe_cli**: Porcentaje de perdida esperada cliente
# MAGIC - **provision**: Monto provision cliente
# MAGIC - **tipo_cli**: Codigo tipo de cliente
# MAGIC - **exposicion**: Monto de exposicion
# MAGIC - **factor_expo**: Porcentaje factor exposicion
# MAGIC - **tip_gar**: Codigo del tipo de garantia cliente
# MAGIC - **mon_gar**: Monto de garantia cliente
# MAGIC - **pvtg**: Porcentaje PVTG
# MAGIC - **pi_metodo_interno**: Porcentaje probabilidad de incumplimiento interno
# MAGIC - **pdi_metodo_interno**: Porcentaje de perdida dada por el incumplimiento interno
# MAGIC - **pe_metodo_interno**: Porcentaje de perdida esperada interno
# MAGIC - **prov_int**: Monto provision metodo interno
# MAGIC - **monto_aval**: Monto avalado del cliente

# COMMAND ----------

vista_cred_fines_generales = """ insert into """ + db_plat_tempX + """.bci10_basenorma_tmp_cred_fines_generales
  select  distinct
          a.periodo_id, 				
          a.rut_cliente, 				
          a.dv_cliente, 				
          a.cod_ope_original, 			
          a.cod_num_operacion, 		
          a.cod_tip_cart, 				
          a.cod_tipo_ope, 				
          a.des_producto_ope, 			
          a.des_banca_ope, 			
          a.cod_cartera_ope,			
          a.mto_deuda_ope,	   			
          a.num_dias_mora_ope,		
          a.ind_cdet, 					
          a.fec_ingreso_deteriodo_ope, 
          a.cod_cctb, 					
          a.fl_ope_reneg,
          17                                        as tipo_activo,
          'CRED. FINES GENERALES'                   as nombre_activo,
          b.pct_pi                                  as pi_cli, 
          b.pct_pdi                                 as pdi_cli,
          b.pct_pe                                  as pe_cli,
          b.mnt_prov_oficial                        as provision,
          case when b.pct_pi is null then 'calificacion'
               else 'GR'
          end                                       as tipo_cli,
          0                                         as exposicion,
          0                                         as factor_expo,
          b.des_tipo_gtia                           as tip_gar,
          b.mnt_garantia                            as mon_gar,
          coalesce(b.pct_ltv, 0.0)                  as pvtg,
          coalesce(b.pct_pi_metodo_interno, 0.0)    as pi_metodo_interno,
          coalesce(b.pct_pdi_metodo_interno, 0.0)   as pdi_metodo_interno,
          coalesce(b.pct_pe_metodo_interno, 0.0)    as pe_metodo_interno,
          coalesce(b.mnt_prov_int, 0.0)             as prov_int,
          coalesce(b.mnt_avalado, 0.0)              as monto_aval		
  from """ + db_plat_tempX + """.tmp_d00_deuda_act_ctg a
  left join """ + db_plat_tempX + """.tmp_modelo_com_gr b
    on a.cod_num_operacion = b.cod_num_operacion
      and a.periodo_id = b.periodo_id
      and a.cod_cartera_ope = 'ACT'
  where a.cod_tipo_ope = 'PLC300'
"""

# COMMAND ----------

sql_safe(vista_cred_fines_generales)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 2: Obtencion Operaciones Creditos Fines Generales Sin Nulos

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion sin campos nulos desde bci10_basenorma_tmp_cred_fines_generales y tmp_modelo_indiv
# MAGIC
# MAGIC **Tabla de salida**: bci10_basenorma_tmp_cred_fines_generales_2 (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM 				
# MAGIC - **rut_cliente**: Rut del cliente 				
# MAGIC - **dv_cliente**: Digito verificador del cliente			
# MAGIC - **cod_ope_original**: Operación Original (TRZ) 			
# MAGIC - **cod_num_operacion**: Número Operación 		
# MAGIC - **cod_tip_cart**: Tipo cartera 				
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00 				
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera 			
# MAGIC - **des_banca_ope**: Descripción operacion banca  			
# MAGIC - **cod_cartera_ope**: Subtipo cartera			
# MAGIC - **mto_deuda_ope**: Saldo total ifrs	   			
# MAGIC - **num_dias_mora_ope**: Dias de mora		
# MAGIC - **ind_cdet**: Indicador cartera deterioro  					
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación 
# MAGIC - **cod_cctb**: Cuenta contable					
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **tipo_activo**: Identificador del tipo activos
# MAGIC - **nombre_activo**: Nombre del tipo activo
# MAGIC - **pi_cli**: Porcentaje probabilidad de incumplimiento cliente
# MAGIC - **pdi_cli**: Porcentaje de perdida dada por el incumplimiento cliente
# MAGIC - **pe_cli**: Porcentaje de perdida esperada cliente
# MAGIC - **provision**: Monto provision cliente
# MAGIC - **tipo_cli**: Codigo tipo de cliente
# MAGIC - **exposicion**: Monto de exposicion
# MAGIC - **factor_expo**: Porcentaje factor exposicion
# MAGIC - **tip_gar**: Codigo del tipo de garantia cliente
# MAGIC - **mon_gar**: Monto de garantia cliente
# MAGIC - **pvtg**: Porcentaje PVTG
# MAGIC - **pi_metodo_interno**: Porcentaje probabilidad de incumplimiento interno
# MAGIC - **pdi_metodo_interno**: Porcentaje de perdida dada por el incumplimiento interno
# MAGIC - **pe_metodo_interno**: Porcentaje de perdida esperada interno
# MAGIC - **prov_int**: Monto provision metodo interno
# MAGIC - **monto_aval**: Monto avalado del cliente

# COMMAND ----------

vista_cred_fines_generales_2 = """ insert into """ + db_plat_tempX + """.bci10_basenorma_tmp_cred_fines_generales_2
  select  distinct
          a.periodo_id, 				
          a.rut_cliente, 				
          a.dv_cliente, 				
          a.cod_ope_original, 			
          a.cod_num_operacion, 		
          a.cod_tip_cart, 				
          a.cod_tipo_ope, 				
          a.des_producto_ope, 			
          a.des_banca_ope, 			
          a.cod_cartera_ope,			
          a.mto_deuda_ope,	   			
          a.num_dias_mora_ope,		
          a.ind_cdet, 					
          a.fec_ingreso_deteriodo_ope, 
          a.cod_cctb, 					
          a.fl_ope_reneg,
          a.tipo_activo,
          a.nombre_activo,
          coalesce(a.pi_cli, b.pct_pi)				        as pi_cli,
          coalesce(a.pdi_cli, b.pct_pdi)				      as pdi_cli,
          coalesce(a.pe_cli, b.pct_pe)				        as pe_cli,
          coalesce(a.provision, b.mnt_provision)		  as provision,
          case when a.tipo_cli = 'calificacion' then b.cod_calificacion
            else a.tipo_cli
          end											                    as tipo_cli,
          a.exposicion,
          a.factor_expo,
          coalesce(a.tip_gar, b.cod_tipo_garantia)	  as tip_gar,
          coalesce(a.mon_gar, b.mnt_garantia)			    as mon_gar,
          a.pvtg,
          a.pi_metodo_interno,
          a.pdi_metodo_interno,
          a.pe_metodo_interno,
          a.prov_int,
          a.monto_aval
  from """ + db_plat_tempX + """.bci10_basenorma_tmp_cred_fines_generales a
  left join """ + db_plat_tempX + """.tmp_modelo_indiv b
    on a.cod_num_operacion = b.cod_num_operacion
      and a.periodo_id = b.periodo_id
      and trim(b.cod_tipo_deu) = 'NCONT'
      and b.mnt_provision <> 0
  where a.periodo_id = """+anomes+"""
"""

# COMMAND ----------

sql_safe(vista_cred_fines_generales_2)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 3: Eliminacion Registros de Fines Generales para Fecha de Proceso

# COMMAND ----------

# MAGIC %md
# MAGIC Se elimina los datos de la tabla base_normativa_v3_ft correspondiente a la fecha de proceso que se va a insertar posteriormente
# MAGIC
# MAGIC **Tabla de salida**: base_normativa_v3_ft (tabla de salida)

# COMMAND ----------

delete_base_normativa_v3_ft = """delete from """ + db_platinumX + """.base_archivos_normativos  where periodo_id = '""" + anomes + """' and cod_tabla_89 = 17  """

# COMMAND ----------

sql_safe(delete_base_normativa_v3_ft)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 4: Insercion Registros FFGG Tabla base_normativa_v3_ft

# COMMAND ----------

# MAGIC %md
# MAGIC Se prepara e inserta los datos a la tabla base_normativa_v3_ft desde la vista bci10_basenorma_tmp_cred_fines_generales_2
# MAGIC
# MAGIC **Tabla de salida**: base_normativa_v3_ft (tabla de salida)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM 				
# MAGIC - **rut_cliente**: Rut del cliente 				
# MAGIC - **dv_cliente**: Digito verificador del cliente			
# MAGIC - **cod_ope_original**: Operación Original (TRZ) 			
# MAGIC - **cod_num_operacion**: Número Operación 		
# MAGIC - **cod_tip_cart**: Tipo cartera 				
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00 				
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera 			
# MAGIC - **des_banca_ope**: Descripción operacion banca  			
# MAGIC - **cod_cartera_ope**: Subtipo cartera			
# MAGIC - **mto_deuda_ope**: Saldo total ifrs	   			
# MAGIC - **num_dias_mora_ope**: Dias de mora		
# MAGIC - **ind_cdet**: Indicador cartera deterioro  					
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación 
# MAGIC - **cod_cctb**: Cuenta contable					
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **cod_tipo_activo**: Codigo tipo activo
# MAGIC - **nom_activo**: Nombre del tipo activo
# MAGIC - **pct_pi**: Porcentaje de la probabilidad incumplimiento
# MAGIC - **pct_pdi**: Porcentaje de perdida dada por el incumplimiento
# MAGIC - **pct_pe**: Porcentaje de perdida esperada
# MAGIC - **mnt_provision**: Monto provisional
# MAGIC - **cod_calif**: Codigo tipo clasificacion cliente
# MAGIC - **mnt_provision**: Monto de exposicion
# MAGIC - **factor_expo**: Porcentaje factor exposicion
# MAGIC - **des_tipo_gtia**: Tiene o no garantia
# MAGIC - **mnt_garantia**: Monto de la garantia
# MAGIC - **pct_ltv**: Porcentaje Loan To Value
# MAGIC - **pct_pi_metodo_interno**: Porcentaje probabilidad de incumplimiento metodo interno
# MAGIC - **pct_pdi_metodo_interno**: Porcentaje de perdida dada por el incumplimiento metodo interno
# MAGIC - **pct_pe_metodo_interno**: Porcentaje de perdida esperada metodo interno
# MAGIC - **mnt_prov_metodo_interno**: Monto provision metodo interno
# MAGIC - **mnt_avalado**: Monto avalado del cliente	
# MAGIC - **fec_proceso**: Fecha de cierre

# COMMAND ----------

insert_base_normativa_v3_ft = """ insert into """ + db_platinumX + """.base_archivos_normativos
  select  periodo_id, 				
          rut_cliente, 				
          dv_cliente, 				
          cod_ope_original, 			
          cod_num_operacion, 		
          cod_tip_cart, 				
          cod_tipo_ope, 				
          des_producto_ope, 			
          des_banca_ope, 			
          cod_cartera_ope,			
          mto_deuda_ope,	   			
          num_dias_mora_ope,		
          ind_cdet, 					
          fec_ingreso_deteriodo_ope, 
          cod_cctb, 					
          fl_ope_reneg,
          -998                              as cod_tabla_34,
          tipo_activo					              as cod_tipo_activo,
          nombre_activo				              as nom_activo,
          pi_cli								            as pct_pi,
          pdi_cli								            as pct_pdi,
          pe_cli								            as pct_pe,
          provision						              as mnt_provision,
          tipo_cli					                as cod_calif,
          exposicion						            as mnt_provision,
          factor_expo,
          tip_gar                           as des_tipo_gtia,
          mon_gar                           as mnt_garantia,
          pvtg							                as pct_ltv,
          pi_metodo_interno				          as pct_pi_metodo_interno,
          pdi_metodo_interno				        as pct_pdi_metodo_interno,
          pe_metodo_interno				          as pct_pe_metodo_interno,
          prov_int				                  as mnt_prov_metodo_interno,
          monto_aval					              as mnt_avalado,			     
          '""" + date_proceso + """'        as fec_proceso
  from """ + db_plat_tempX + """.bci10_basenorma_tmp_cred_fines_generales_2
"""

# COMMAND ----------

sql_safe(insert_base_normativa_v3_ft)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Conteo de registros insertados en tabla de salida

# COMMAND ----------

registros = spark.sql(""" select count(1) as cant_registros from """ + db_platinumX + """.base_archivos_normativos  where periodo_id = """ + anomes + """ and cod_tabla_89 = 17   """)

reg_insertados = registros.toPandas()
cantidad = reg_insertados.iloc[0]["cant_registros"]

print("Se Insertaron " + str(cantidad) +" Registros en la tabla temporal base_archivos_normativos para el activo 17")

# COMMAND ----------

# MAGIC %md
# MAGIC ## Mensaje Final

# COMMAND ----------

dbutils.notebook.exit("{\"coderror\":\"0\", \"msgerror\":\"Notebook termina ejecucion satisfactoriamente\"}")